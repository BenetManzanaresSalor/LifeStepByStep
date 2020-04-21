using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LC_GenericTerrain<Cell> : MonoBehaviour where Cell : LC_Cell
{
	#region Attributes

	#region Constants

	public const int MaxVerticesByMesh = 65536;

	#endregion

	#region Settings	

	[Header( "Global settings" )]
	[SerializeField] protected Vector3 CellSize = Vector3.one;

	[Header( "Render settings" )]
	[SerializeField] protected Material RenderMaterial;
	[SerializeField] protected Vector2Int TextureColumnsAndRows = Vector2Int.one;
	[SerializeField] [Range( 1, 4 )] protected float TextureMarginRelation = 3;
	[SerializeField] protected bool UseSplitAndMerge;

	#endregion

	#region Function attributes

	protected Cell[,] Cells;

	protected GameObject RenderParent;
	protected int MaxVerticesPerRenderElem = 12;
	protected List<Vector3> currentVertices;
	protected List<int> currentTriangles;
	protected List<Vector2> currentUVs;

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		CreateTerrain();
	}

	protected virtual void CreateTerrain()
	{
		CreateCells();
		CreateRender();
	}

	protected virtual void CreateCells()
	{
		CreateMap( out Vector2Int terrainDimensions );

		Cells = new Cell[terrainDimensions.x, terrainDimensions.y];
		for ( int x = 0; x < Cells.GetLength( 0 ); x++ )
		{
			for ( int z = 0; z < Cells.GetLength( 1 ); z++ )
			{
				Cells[x, z] = CreateCell( x, z );
			}
		}
	}

	protected abstract void CreateMap( out Vector2Int terrainDimensions );

	protected abstract Cell CreateCell(int x, int z);

	protected virtual void CreateRender()
	{
		// Create render parent
		RenderParent = new GameObject();
		RenderParent.transform.parent = this.transform;
		RenderParent.name = "Render";

		// Initialize render lists
		currentVertices = new List<Vector3>();
		currentTriangles = new List<int>();
		currentUVs = new List<Vector2>();

		// Render all elements
		RenderElements();

		// Create mesh if some vertices remains
		if ( currentVertices.Count > 0 )
		{
			CreateRenderObject();
		}
	}

	#endregion

	#region Render

	protected virtual void RenderElements()
	{
		if ( UseSplitAndMerge )
		{
			SplitAndMergeRender();
		}
		else
		{
			foreach ( Cell cell in Cells )
			{
				CreateElementRender( cell, cell );

				// Create mesh before get the maximum mesh vertices at next cell render
				if ( currentVertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
				{
					CreateRenderObject();
				}
			}
		}			
	}

	protected virtual void SplitAndMergeRender()
	{
		List<MathFunctions.QuadTreeSector> sectors = MathFunctions.QuadTree(
			( x, z ) => { return Cells[x, z].TerrainPosition.y; },
			( x, y ) => { return x == y; },
			Cells.GetLength(0), true );

		foreach ( MathFunctions.QuadTreeSector sector in sectors )
		{
			CreateElementRender( Cells[sector.Initial.x, sector.Initial.y],
				Cells[sector.Final.x, sector.Final.y] );

			// Create mesh before get the maximum mesh vertices at next cell render
			if ( currentVertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
			{
				CreateRenderObject();
			}
		}
	}

	protected virtual void CreateElementRender( Cell iniCell, Cell endCell )
	{
		Vector3Int iniCellPos = iniCell.TerrainPosition;
		Vector3Int endCellPos = endCell.TerrainPosition;

		Vector3 currentCellPos = ( TerrainToRealPos( iniCellPos ) + TerrainToRealPos( endCellPos ) ) / 2f;
		int numXCells = endCellPos.x - iniCellPos.x + 1;
		int numZCells = endCellPos.z - iniCellPos.z + 1;

		// Set vertices
		currentVertices.Add( currentCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		currentVertices.Add( currentCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		currentVertices.Add( currentCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );
		currentVertices.Add( currentCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );

		// Set triangles
		currentTriangles.Add( currentVertices.Count - 4 );
		currentTriangles.Add( currentVertices.Count - 1 );
		currentTriangles.Add( currentVertices.Count - 2 );

		currentTriangles.Add( currentVertices.Count - 2 );
		currentTriangles.Add( currentVertices.Count - 3 );
		currentTriangles.Add( currentVertices.Count - 4 );

		// UVs
		GetUVs( iniCellPos, out Vector2 iniUV, out Vector2 endUV );
		currentUVs.Add( new Vector2( iniUV.x, endUV.y ) );
		currentUVs.Add( new Vector2( endUV.x, endUV.y ) );
		currentUVs.Add( new Vector2( endUV.x, iniUV.y ) );
		currentUVs.Add( new Vector2( iniUV.x, iniUV.y ) );

		// Positive x border
		if ( endCellPos.x < Cells.GetLength(0) - 1 )
		{
			for ( int z = 0; z < numZCells; z++ )
			{
				currentCellPos = TerrainToRealPos( Cells[endCellPos.x, endCellPos.z - z].TerrainPosition );
				CreateEdgeRender( currentCellPos, Cells[endCellPos.x + 1, endCellPos.z - z], true, iniUV, endUV );
			}
		}

		// Positive z border
		if ( endCellPos.z < Cells.GetLength(1) - 1 )
		{
			for ( int x = 0; x < numXCells; x++ )
			{
				currentCellPos = TerrainToRealPos( Cells[endCellPos.x - x, endCellPos.z].TerrainPosition );
				CreateEdgeRender( currentCellPos, Cells[endCellPos.x - x, endCellPos.z + 1], false, iniUV, endUV );
			}
		}		
	}

	public void GetUVs( Vector3Int pos, out Vector2 ini, out Vector2 end )
	{
		Vector2Int texPos = GetTexturePos( Cells[pos.x, pos.z] );
		
		Vector2 textureSize = new Vector2( 1f / TextureColumnsAndRows.x, 1f / TextureColumnsAndRows.y ); // TODO : Precalcule this
		Vector2 margin = textureSize / TextureMarginRelation; // TODO : Precalcule this

		end = new Vector2( ( texPos.x + 1f ) / TextureColumnsAndRows.x, ( texPos.y + 1f ) / TextureColumnsAndRows.y ) - margin;
		ini = end - margin;
	}

	protected abstract Vector2Int GetTexturePos( Cell cell );

	protected virtual void CreateEdgeRender( Vector3 cellRealPos, Cell edgeCell, bool toRight, Vector2 iniUV, Vector2 endUV )
	{
		Vector2 edgeIniUV;
		Vector2 edgeEndUV;
		float edgeCellHeightDiff = TerrainToRealPos( edgeCell.TerrainPosition ).y - cellRealPos.y;

		if ( edgeCellHeightDiff != 0 )
		{
			float xMultipler = 1;
			float zMultipler = -1;
			if ( !toRight )
			{
				xMultipler = -1;
				zMultipler = 1;
			}

			// Set edge vertexs
			currentVertices.Add( cellRealPos + new Vector3( CellSize.x * xMultipler / 2f, edgeCellHeightDiff, CellSize.z * zMultipler / 2f ) );
			currentVertices.Add( cellRealPos + new Vector3( CellSize.x / 2f, edgeCellHeightDiff, CellSize.z / 2f ) );
			currentVertices.Add( cellRealPos + new Vector3( CellSize.x / 2f, 0, CellSize.z / 2f ) );
			currentVertices.Add( cellRealPos + new Vector3( CellSize.x * xMultipler / 2f, 0, CellSize.z * zMultipler / 2f ) );

			// Set edge triangles
			if ( toRight )
			{
				currentTriangles.Add( currentVertices.Count - 4 );
				currentTriangles.Add( currentVertices.Count - 1 );
				currentTriangles.Add( currentVertices.Count - 2 );

				currentTriangles.Add( currentVertices.Count - 2 );
				currentTriangles.Add( currentVertices.Count - 3 );
				currentTriangles.Add( currentVertices.Count - 4 );
			}
			// Inverted ( needed to be seen )
			else
			{
				currentTriangles.Add( currentVertices.Count - 2 );
				currentTriangles.Add( currentVertices.Count - 1 );
				currentTriangles.Add( currentVertices.Count - 4 );

				currentTriangles.Add( currentVertices.Count - 4 );
				currentTriangles.Add( currentVertices.Count - 3 );
				currentTriangles.Add( currentVertices.Count - 2 );
			}

			// Set edge UVs dependently of the height difference
			if ( edgeCellHeightDiff < 0 )
			{
				edgeIniUV = iniUV;
				edgeEndUV = endUV;
			}
			else
			{
				GetUVs( edgeCell.TerrainPosition, out edgeIniUV, out edgeEndUV );
			}
			currentUVs.Add( new Vector2( edgeIniUV.x, edgeEndUV.y ) );
			currentUVs.Add( new Vector2( edgeEndUV.x, edgeEndUV.y ) );
			currentUVs.Add( new Vector2( edgeEndUV.x, edgeIniUV.y ) );
			currentUVs.Add( new Vector2( edgeIniUV.x, edgeIniUV.y ) );
		}
	}

	protected virtual void CreateRenderObject()
	{
		// Create render object
		GameObject render = new GameObject();
		render.transform.parent = RenderParent.transform;
		MeshFilter renderMeshFilter = render.AddComponent<MeshFilter>();
		render.AddComponent<MeshRenderer>().material = RenderMaterial;
		MeshCollider renderMeshCollider = render.AddComponent<MeshCollider>();

		// Set meshes
		Mesh worldMesh = new Mesh
		{
			vertices = currentVertices.ToArray(),
			triangles = currentTriangles.ToArray(),
			uv = currentUVs.ToArray()
		};
		worldMesh.RecalculateBounds();
		worldMesh.RecalculateNormals();
		worldMesh.Optimize();

		renderMeshFilter.mesh = worldMesh;
		renderMeshCollider.sharedMesh = worldMesh;

		// Reset lists
		currentVertices.Clear();
		currentTriangles.Clear();
		currentUVs.Clear();
	}

	#endregion

	#region Terrain methods

	public virtual bool IsPosInTerrain( Vector2Int pos )
	{
		bool isIn = true;

		if ( pos.x >= Cells.GetLength(0) || pos.x < 0 )
		{
			isIn = false;
		}
		else if ( pos.y >= Cells.GetLength(1) || pos.y < 0 )
		{
			isIn = false;
		}

		return isIn;
	}

	public virtual Vector3 TerrainToRealPos( Vector3Int pos )
	{
		return transform.position + new Vector3( pos.x * CellSize.x, pos.y * CellSize.y, pos.z * CellSize.z );
	}

	public virtual Cell GetCell( Vector2Int pos )
	{
		return IsPosInTerrain( pos ) ? Cells[pos.x, pos.y] : null;
	}

	public virtual Vector3 GetIniCellPos()
	{
		return TerrainToRealPos( Cells[0, 0].TerrainPosition );
	}
	
	#endregion
}
