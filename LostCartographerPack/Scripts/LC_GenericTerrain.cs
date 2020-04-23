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
	[SerializeField] [Range( 1, 8 )] protected int ChunkSizeLevel = 4;
	[SerializeField] [Range( 0, 128 )] protected int ChunkRenderDistance = 4;

	[Header( "Render settings" )]
	[SerializeField] protected Material RenderMaterial;
	[SerializeField] protected Vector2Int TextureColumnsAndRows = Vector2Int.one;
	[SerializeField] [Range( 1, 4 )] protected float TextureMarginRelation = 3;
	[SerializeField] protected bool UseSplitAndMerge;

	#endregion

	#region Function attributes

	protected int ChunkSize;
	protected GameObject[,] Chunks;
	protected int MaxVerticesPerRenderElem = 12;
	protected Vector2 TextureSize;
	protected Vector2 TextureMargin;
	protected List<Vector3> currentVertices;
	protected List<int> currentTriangles;
	protected List<Vector2> currentUVs;

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		ChunkSize = (int)Mathf.Pow( 2, ChunkSizeLevel );
		TextureSize = new Vector2( 1f / TextureColumnsAndRows.x, 1f / TextureColumnsAndRows.y );
		TextureMargin = TextureSize / TextureMarginRelation;
		CreateTerrain();
	}

	protected virtual void CreateTerrain()
	{
		CreateChunk( Vector2Int.zero );

		foreach ( Vector2Int pos in MathFunctions.NearlyPositions( Vector2Int.zero, (uint)ChunkRenderDistance ) )
		{
			CreateChunk( pos );
		}
	}

	protected virtual void CreateChunk( Vector2Int chunkPos )
	{
		LC_Chunk chunk = new LC_Chunk( new GameObject(), chunkPos * ChunkSize );
		chunk.Obj.transform.parent = this.transform;
		chunk.Obj.name = "Chunk_" + chunkPos;
		chunk.Obj.transform.position = TerrainPosToReal( new Vector3Int( chunk.CellsOffset.x, 0, chunk.CellsOffset.y ) );

		Cell[,] cells = CreateCells( chunk.CellsOffset );
		CreateMesh( chunk, cells );
	}

	protected virtual Cell[,] CreateCells( Vector2Int chunkOffset )
	{
		Cell[,] cells = new Cell[ChunkSize, ChunkSize];
		for ( int x = 0; x < ChunkSize; x++ )
		{
			for ( int z = 0; z < ChunkSize; z++ )
			{
				cells[x, z] = CreateCell( x + chunkOffset.x, z + chunkOffset.y );
			}
		}

		return cells;
	}

	public abstract Cell CreateCell( int x, int z );

	protected virtual void CreateMesh( LC_Chunk chunk, Cell[,] cells )
	{
		// Initialize render lists
		currentVertices = new List<Vector3>();
		currentTriangles = new List<int>();
		currentUVs = new List<Vector2>();

		// Render all elements
		CellsToMesh( chunk, cells );

		// Create mesh if some vertices remains
		if ( currentVertices.Count > 0 )
		{
			CreateMeshObj( chunk.Obj );
		}
	}

	#endregion

	#region Render

	protected virtual void CellsToMesh( LC_Chunk chunk, Cell[,] cells )
	{
		if ( UseSplitAndMerge )
		{
			SplitAndMergeMesh( chunk, cells );
		}
		else
		{
			foreach ( Cell cell in cells )
			{
				Vector2Int cellPosInChunk = chunk.CellPosToChunk( cell.TerrainPos );
				CreateElementMesh( cellPosInChunk, cellPosInChunk, chunk, cells );

				// Create mesh before get the maximum mesh vertices at next cell render
				if ( currentVertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
				{
					CreateMeshObj( chunk.Obj );
				}
			}
		}
	}

	protected virtual void SplitAndMergeMesh( LC_Chunk chunk, Cell[,] cells )
	{
		List<MathFunctions.QuadTreeSector> sectors = MathFunctions.QuadTree(
			( x, z ) => { return cells[x, z].TerrainPos.y; },
			( x, y ) => { return x == y; },
			ChunkSize, true );

		foreach ( MathFunctions.QuadTreeSector sector in sectors )
		{
			CreateElementMesh( sector.Initial, sector.Final, chunk, cells );

			// Create mesh before get the maximum mesh vertices at next cell render
			if ( currentVertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
			{
				CreateMeshObj( chunk.Obj );
			}
		}
	}

	protected virtual void CreateElementMesh( Vector2Int iniCellPos, Vector2Int endCellPos, LC_Chunk chunk, Cell[,] cells )
	{
		Vector3 realCellPos = ( TerrainPosToReal( cells[iniCellPos.x, iniCellPos.y].TerrainPos ) +
			TerrainPosToReal( cells[endCellPos.x, endCellPos.y].TerrainPos ) ) / 2f;
		int numXCells = endCellPos.x - iniCellPos.x + 1;
		int numZCells = endCellPos.y - iniCellPos.y + 1;

		// Set vertices
		currentVertices.Add( realCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		currentVertices.Add( realCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		currentVertices.Add( realCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );
		currentVertices.Add( realCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );

		// Set triangles
		currentTriangles.Add( currentVertices.Count - 4 );
		currentTriangles.Add( currentVertices.Count - 1 );
		currentTriangles.Add( currentVertices.Count - 2 );

		currentTriangles.Add( currentVertices.Count - 2 );
		currentTriangles.Add( currentVertices.Count - 3 );
		currentTriangles.Add( currentVertices.Count - 4 );

		// UVs
		CreateUVs( iniCellPos, out Vector2 iniUV, out Vector2 endUV, chunk, cells );
		currentUVs.Add( new Vector2( iniUV.x, endUV.y ) );
		currentUVs.Add( new Vector2( endUV.x, endUV.y ) );
		currentUVs.Add( new Vector2( endUV.x, iniUV.y ) );
		currentUVs.Add( new Vector2( iniUV.x, iniUV.y ) );

		// Positive x border
		if ( endCellPos.x < cells.GetLength( 0 ) - 1 )
		{
			for ( int z = 0; z < numZCells; z++ )
			{
				realCellPos = TerrainPosToReal( cells[endCellPos.x, endCellPos.y - z].TerrainPos );
				CreateEdgeMesh( realCellPos, cells[endCellPos.x + 1, endCellPos.y - z], true, iniUV, endUV, chunk, cells );
			}
		}

		// Positive z border
		if ( endCellPos.y < cells.GetLength( 1 ) - 1 )
		{
			for ( int x = 0; x < numXCells; x++ )
			{
				realCellPos = TerrainPosToReal( cells[endCellPos.x - x, endCellPos.y].TerrainPos );
				CreateEdgeMesh( realCellPos, cells[endCellPos.x - x, endCellPos.y + 1], false, iniUV, endUV, chunk, cells );
			}
		}
	}

	public void CreateUVs( Vector2Int pos, out Vector2 ini, out Vector2 end, LC_Chunk chunk, Cell[,] cells )
	{
		Vector2Int texPos = CreateTexPos( cells[pos.x, pos.y], chunk, cells );

		end = new Vector2( ( texPos.x + 1f ) / TextureColumnsAndRows.x, ( texPos.y + 1f ) / TextureColumnsAndRows.y ) - TextureMargin;
		ini = end - TextureMargin;
	}

	protected abstract Vector2Int CreateTexPos( Cell cell, LC_Chunk chunk, Cell[,] cells );

	protected virtual void CreateEdgeMesh( Vector3 cellRealPos, Cell edgeCell, bool toRight, Vector2 iniUV, Vector2 endUV, LC_Chunk chunk, Cell[,] cells )
	{
		Vector2 edgeIniUV;
		Vector2 edgeEndUV;
		float edgeCellHeightDiff = TerrainPosToReal( edgeCell.TerrainPos ).y - cellRealPos.y;

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
				Vector2Int cellPosInChunk = new Vector2Int( edgeCell.TerrainPos.x - chunk.CellsOffset.x, edgeCell.TerrainPos.z - chunk.CellsOffset.y );
				CreateUVs( cellPosInChunk, out edgeIniUV, out edgeEndUV, chunk, cells );
			}
			currentUVs.Add( new Vector2( edgeIniUV.x, edgeEndUV.y ) );
			currentUVs.Add( new Vector2( edgeEndUV.x, edgeEndUV.y ) );
			currentUVs.Add( new Vector2( edgeEndUV.x, edgeIniUV.y ) );
			currentUVs.Add( new Vector2( edgeIniUV.x, edgeIniUV.y ) );
		}
	}

	protected virtual void CreateMeshObj( GameObject chunkObj )
	{
		// Create render object
		GameObject render = new GameObject();
		render.transform.parent = chunkObj.transform;
		render.name = "Render";
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

	#region Auxiliar

	public virtual Vector3 TerrainPosToReal( Vector3Int pos )
	{
		return transform.position + new Vector3( pos.x * CellSize.x, pos.y * CellSize.y, pos.z * CellSize.z );
	}

	// TODO : Maybe delete
	protected virtual Vector3 ChunkToRealPos( Vector3Int pos, GameObject chunkObj )
	{
		return chunkObj.transform.position + new Vector3( pos.x * CellSize.x, pos.y * CellSize.y, pos.z * CellSize.z );
	}

	protected virtual Cell GetChunkCell( Vector2Int pos, Cell[,] cells )
	{
		bool isIn = pos.x >= 0 && pos.x < ChunkSize && pos.y >= 0 && pos.y < ChunkSize;
		return isIn ? cells[pos.x, pos.y] : null;
	}

	#endregion
}
