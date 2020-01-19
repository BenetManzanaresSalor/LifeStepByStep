using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public abstract class WorldMaster : MonoBehaviour
{
	#region Attributes

	#region Constants

	public const int VertexsByBaseCell = 24;
	public const int MaxVerticesByMesh = 65536;

	#endregion

	#region Settings	

	[Header( "World master global settings" )]
	[SerializeField] protected float CellXsize;
	[SerializeField] protected float CellYsize;
	[SerializeField] protected float CellZsize;
	[SerializeField] protected Vector2Int MinAndMaxHeights = new Vector2Int( 0, 1 );
	[SerializeField] protected WorldObject[] WorldObjects;

	[Header( "World master render settings" )]
	[SerializeField] protected WorldCellType DefaultWorldCellType = WorldCellType.GROUND;
	[SerializeField] protected Material RenderMaterial;
	[SerializeField] protected Vector2Int TextureColumnsAndRows = Vector2Int.one;
	[SerializeField] [Range( 3, 10 )] protected float TextureMarginRelation = 3;
	[SerializeField] protected WorldCellRenderType RendererType = WorldCellRenderType.EFFECTORS;
	[SerializeField]
	[Range( 0, 10 )]
	protected int EffectorsRange;
	[SerializeField] protected bool UseQuadTreeCells;
	[SerializeField] protected bool MergeQuadTreeCells;

	#endregion

	#region Function attributes

	protected int Xsize;
	protected int Zsize;
	protected WorldCell[,] World;

	protected GameObject RenderParent;
	List<Vector3> vertices;
	List<int> triangles;
	List<Vector2> uvs;

	protected Stopwatch Chrono;

	#endregion

	#region Data accesors

	public bool IsAutomaticStepingEnabled { get; protected set; }

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		Chrono = new Stopwatch();
		CreateWorld();
	}

	protected virtual void CreateWorld()
	{
		IsAutomaticStepingEnabled = false;

		Chrono.Start();

		CreateMap();

		// Create world cells
		World = new WorldCell[Xsize, Zsize];
		for ( int x = 0; x < Xsize; x++ )
		{
			for ( int z = 0; z < Zsize; z++ )
			{
				World[x, z] = CreateWorldCell( x, z );
			}
		}

		// Render world
		Render();

		// Show creation time
		Chrono.Stop();
		UnityEngine.Debug.Log( $"World created in time : {Chrono.Elapsed}" );
		Chrono.Reset();
	}

	protected abstract void CreateMap();

	protected abstract WorldCell CreateWorldCell( int x, int z );

	#region Render

	protected virtual void Render()
	{
		// Create render parent
		RenderParent = new GameObject();
		RenderParent.transform.parent = this.transform;
		RenderParent.name = "Render";

		// Initialize render lists
		vertices = new List<Vector3>();
		triangles = new List<int>();
		uvs = new List<Vector2>();

		if ( UseQuadTreeCells )
		{
			QuadTreeCells();
		}
		else
		{
			foreach ( WorldCell cell in World )
			{
				CreateCellRender( cell.WorldPosition3D, cell.WorldPosition3D );
			}
		}

		// Create mesh if some vertices remains
		if ( vertices.Count > 0 )
		{
			CreateRenderObject();
		}
	}

	protected virtual void CreateCellRender( Vector3Int iniPos, Vector3Int endPos )
	{
		WorldCellType type = World[iniPos.x, iniPos.z].Type;
		Vector3 currentCellPos = ( WorldToRealPosition( iniPos ) + WorldToRealPosition( endPos ) ) / 2f;
		int numXCells = endPos.x - iniPos.x + 1;
		int numZCells = endPos.z - iniPos.z + 1;

		// Set vertexs
		vertices.Add( currentCellPos + new Vector3( -CellXsize * numXCells / 2f, 0, -CellZsize * numZCells / 2f ) );
		vertices.Add( currentCellPos + new Vector3( CellXsize * numXCells / 2f, 0, -CellZsize * numZCells / 2f ) );
		vertices.Add( currentCellPos + new Vector3( CellXsize * numXCells / 2f, 0, CellZsize * numZCells / 2f ) );
		vertices.Add( currentCellPos + new Vector3( -CellXsize * numXCells / 2f, 0, CellZsize * numZCells / 2f ) );

		// Set triangles
		triangles.Add( vertices.Count - 4 );
		triangles.Add( vertices.Count - 1 );
		triangles.Add( vertices.Count - 2 );

		triangles.Add( vertices.Count - 2 );
		triangles.Add( vertices.Count - 3 );
		triangles.Add( vertices.Count - 4 );

		// UVs
		GetUVs( iniPos, type, out Vector2 iniUV, out Vector2 endUV );
		uvs.Add( new Vector2( iniUV.x, endUV.y ) );
		uvs.Add( new Vector2( endUV.x, endUV.y ) );
		uvs.Add( new Vector2( endUV.x, iniUV.y ) );
		uvs.Add( new Vector2( iniUV.x, iniUV.y ) );

		// Positive x border
		if ( endPos.x < Xsize - 1 )
		{
			for ( int z = 0; z < numZCells; z++ )
			{
				currentCellPos = WorldToRealPosition( World[endPos.x, endPos.z - z].WorldPosition3D );
				CreateBorderRender( currentCellPos, World[endPos.x + 1, endPos.z - z], true, iniUV, endUV );
			}
		}

		// Positive z border
		if ( endPos.z < Zsize - 1 )
		{
			for ( int x = 0; x < numXCells; x++ )
			{
				currentCellPos = WorldToRealPosition( World[endPos.x - x, endPos.z].WorldPosition3D );
				CreateBorderRender( currentCellPos, World[endPos.x - x, endPos.z + 1], false, iniUV, endUV );
			}
		}

		// Create mesh before get the maximum mesh vertices in next cell render
		if ( vertices.Count + 12 >= MaxVerticesByMesh )
		{
			CreateRenderObject();
		}
	}

	protected virtual void CreateBorderRender( Vector3 cellRealPos, WorldCell borderCell, bool toRight, Vector2 iniUV, Vector2 endUV )
	{
		Vector2 borderIniUV;
		Vector2 borderEndUV;
		float borderCellHeightDiff = WorldToRealPosition( borderCell.WorldPosition3D ).y - cellRealPos.y;

		if ( borderCellHeightDiff != 0 )
		{
			float xMultipler = 1;
			float zMultipler = -1;
			if ( !toRight )
			{
				xMultipler = -1;
				zMultipler = 1;
			}

			// Set border vertexs
			vertices.Add( cellRealPos + new Vector3( CellXsize * xMultipler / 2f, borderCellHeightDiff, CellZsize * zMultipler / 2f ) );
			vertices.Add( cellRealPos + new Vector3( CellXsize / 2f, borderCellHeightDiff, CellZsize / 2f ) );
			vertices.Add( cellRealPos + new Vector3( CellXsize / 2f, 0, CellZsize / 2f ) );
			vertices.Add( cellRealPos + new Vector3( CellXsize * xMultipler / 2f, 0, CellZsize * zMultipler / 2f ) );

			// Set border triangles
			if ( toRight )
			{
				triangles.Add( vertices.Count - 4 );
				triangles.Add( vertices.Count - 1 );
				triangles.Add( vertices.Count - 2 );

				triangles.Add( vertices.Count - 2 );
				triangles.Add( vertices.Count - 3 );
				triangles.Add( vertices.Count - 4 );
			}
			// Inverted ( needed to be seen )
			else
			{
				triangles.Add( vertices.Count - 2 );
				triangles.Add( vertices.Count - 1 );
				triangles.Add( vertices.Count - 4 );

				triangles.Add( vertices.Count - 4 );
				triangles.Add( vertices.Count - 3 );
				triangles.Add( vertices.Count - 2 );
			}

			// Set border UVs dependently of the height difference
			if ( borderCellHeightDiff < 0 )
			{
				borderIniUV = iniUV;
				borderEndUV = endUV;
			}
			else
			{
				GetUVs( borderCell.WorldPosition3D, borderCell.Type, out borderIniUV, out borderEndUV );
			}
			uvs.Add( new Vector2( borderIniUV.x, borderEndUV.y ) );
			uvs.Add( new Vector2( borderEndUV.x, borderEndUV.y ) );
			uvs.Add( new Vector2( borderEndUV.x, borderIniUV.y ) );
			uvs.Add( new Vector2( borderIniUV.x, borderIniUV.y ) );
		}
	}

	public void GetUVs( Vector3Int pos, WorldCellType type, out Vector2 ini, out Vector2 end )
	{
		float value = 0;

		switch ( RendererType )
		{
			case WorldCellRenderType.EFFECTORS:
				value = World[pos.x, pos.z].ValueByEffectors( v =>
				{
					return IsPositionInWorld( v ) ? World[v.x, v.y] : null;
				}, EffectorsRange );
				break;
			case WorldCellRenderType.HEIGHT:
				value = InverseLerpHeight( pos.y, type );
				break;
		}

		Vector2 textureSize = new Vector2( 1f / TextureColumnsAndRows.x, 1f / TextureColumnsAndRows.y );
		Vector2 margin = textureSize / TextureMarginRelation;

		int x = 0;
		switch ( type )
		{
			case WorldCellType.GROUND:
				x = 0;
				break;
			case WorldCellType.WATER:
				x = 1;
				break;
		}
		int y = (int)MathFunctions.Clamp( TextureColumnsAndRows.y * value, 0, TextureColumnsAndRows.y - 1 );

		end = new Vector2( ( x + 1f ) / TextureColumnsAndRows.x, ( y + 1f ) / TextureColumnsAndRows.y ) - margin;
		ini = end - margin;
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
			vertices = vertices.ToArray(),
			triangles = triangles.ToArray(),
			uv = uvs.ToArray()
		};
		worldMesh.RecalculateBounds();
		worldMesh.RecalculateNormals();
		worldMesh.Optimize();

		renderMeshFilter.mesh = worldMesh;
		renderMeshCollider.sharedMesh = worldMesh;

		// Reset lists
		vertices.Clear();
		triangles.Clear();
		uvs.Clear();
	}

	protected virtual void QuadTreeCells()
	{
		List<MathFunctions.QuadTreeSector> sectors;

		sectors = MathFunctions.QuadTree(
			( x, z ) => { return World[x, z].WorldPosition3D.y; },
			( x, y ) => { return x == y; },
			Xsize, MergeQuadTreeCells );

		foreach ( MathFunctions.QuadTreeSector sector in sectors )
		{
			CreateCellRender( World[sector.Initial.x, sector.Initial.y].WorldPosition3D,
				World[sector.Final.x, sector.Final.y].WorldPosition3D );
		}
	}

	// TODO : WorldCell interpolated render
	/* GROUND

	protected override void OnEffectors( WorldBase closestEffector, float closestEffectorDistance, List<WorldBase> effectorsInRange )
	{
		base.OnEffectors( closestEffector, closestEffectorDistance, effectorsInRange );

		// Choose affected directions
		bool[] directions = new bool[] { false, false, false, false };

		// If some effector is touching in one of the four directions
		if ( closestEffectorDistance == 1 )
		{
			foreach ( WorldBase effector in effectorsInRange )
			{
				for ( int i = 0; i < 4; i++ )
				{
					if ( effector.WorldPosition2D == WorldPosition2D + MathFunctions.FourDirections2D[i] )
					{
						directions[i] = true;
					}
				}
			}
		}

		AdaptMesh( directions );
	}

	protected virtual void AdaptMesh( bool[] directions )
	{
		// Adapt mesh to effectors
		Mesh = new Mesh();

		// VERTICES
		float[] Xmultipliers = new float[4] { 0, 0, 0, 0 };
		float[] Zmultipliers = new float[4] { 0, 0, 0, 0 };
		if ( directions[0] )
		{
			Zmultipliers[2] = 1;
			Zmultipliers[3] = 1;
		}
		if ( directions[1] )
		{
			Xmultipliers[1] = 1;
			Xmultipliers[3] = 1;
		}
		if ( directions[2] )
		{
			Zmultipliers[0] = 1;
			Zmultipliers[1] = 1;
		}
		if ( directions[3] )
		{
			Xmultipliers[0] = 1;
			Xmultipliers[2] = 1;
		}

		Vector3 offset = new Vector3( -0.5f, -0.5f, -0.5f );
		Vector3[] vertices = new Vector3[]
		{
			offset + new Vector3( 1, 0, 1 ),
			offset + new Vector3( 0, 0, 1 ),
			offset + new Vector3( 1 - TopAreaMultiplier * Xmultipliers[3], 1, 1 - TopAreaMultiplier * Zmultipliers[3] ), // Top right
			offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[2], 1, 1 - TopAreaMultiplier * Zmultipliers[2] ), // Top left

			offset + new Vector3( 1 - TopAreaMultiplier * Xmultipliers[1], 1, 0 + TopAreaMultiplier * Zmultipliers[1] ), // Bottom right
			offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[0], 1, 0 + TopAreaMultiplier * Zmultipliers[0] ), // Bottom left
			offset + new Vector3( 1, 0, 0 ),
			offset + new Vector3( 0, 0, 0 ),

			offset + new Vector3( 1 - TopAreaMultiplier * Xmultipliers[3], 1, 1 - TopAreaMultiplier * Zmultipliers[3] ), // Top right
			offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[2], 1, 1 - TopAreaMultiplier * Zmultipliers[2] ), // Top left
			offset + new Vector3( 1 - TopAreaMultiplier * Xmultipliers[1], 1, 0 + TopAreaMultiplier * Zmultipliers[1] ), // Bottom right
			offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[0], 1, 0 + TopAreaMultiplier * Zmultipliers[0] ), // Bottom left

			offset + new Vector3( 1, 0, 0 ),
			offset + new Vector3( 1, 0, 1 ),
			offset + new Vector3( 0, 0, 1 ),
			offset + new Vector3( 0, 0, 0 ),

			offset + new Vector3( 0, 0, 1 ),
			offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[2], 1, 1 - TopAreaMultiplier * Zmultipliers[2] ), // Top left
			offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[0], 1, 0 + TopAreaMultiplier * Zmultipliers[0] ), // Bottom left
			offset + new Vector3( 0, 0, 0 ),

			offset + new Vector3( 1, 0, 0 ),
			offset + new Vector3( 1 - TopAreaMultiplier * Xmultipliers[1], 1, 0 + TopAreaMultiplier * Zmultipliers[1] ), // Bottom right
			offset + new Vector3( 1 - TopAreaMultiplier * Xmultipliers[3], 1, 1 - TopAreaMultiplier * Zmultipliers[3] ), // Top right
			offset + new Vector3( 1, 0, 1 ),

			//offset + new Vector3( 0 + TopAreaMultiplier * Xmultipliers[0], 1, TopAreaMultiplier * Zmultipliers[0] ),
			//offset + new Vector3( 1 - TopAreaMultiplier *  Xmultipliers[1], 1, 0 + TopAreaMultiplier *  Zmultipliers[1] ),
			//offset + new Vector3( 0 + TopAreaMultiplier *  Xmultipliers[2], 1, 1 - TopAreaMultiplier *  Zmultipliers[2]),
			//offset + new Vector3( 1  - TopAreaMultiplier *  Xmultipliers[3], 1, 1  - TopAreaMultiplier *  Zmultipliers[3] )
		};

		Mesh.vertices = vertices;

		// TRIANGLES
		Mesh.triangles = new int[]
		{
			0, 2, 3,
			0, 3, 1,
			8, 4, 5,
			8, 5, 9,
			10, 6, 7,
			10, 7, 11,
			12, 13, 14,
			12, 14, 15,
			16, 17, 18,
			16, 18, 19,
			20, 21, 22,
			20, 22, 23
		};

		Mesh.uv = new Vector2[]
		{
			new Vector2( 0, 0 ),
			new Vector2( 1, 0 ),
			new Vector2( 0, 1 ),
			new Vector2( 1, 1 ),

			new Vector2( 0, 1 ),
			new Vector2( 1, 1 ),
			new Vector2( 0, 1 ),
			new Vector2( 1, 1 ),

			new Vector2( 0, 0 ),
			new Vector2( 1, 0 ),
			new Vector2( 0, 0 ),
			new Vector2( 1, 0 ),

			new Vector2( 0, 0 ),
			new Vector2( 0, 1 ),
			new Vector2( 1, 1 ),
			new Vector2( 1, 0 ),

			new Vector2( 0, 0 ),
			new Vector2( 0, 1 ),
			new Vector2( 1, 1 ),
			new Vector2( 1, 0 ),

			new Vector2( 0, 0 ),
			new Vector2( 0, 1 ),
			new Vector2( 1, 1 ),
			new Vector2( 1, 0 ),
		};

		Mesh.RecalculateNormals();
		Mesh.RecalculateTangents();

		MeshFilter.mesh = Mesh;
	}
		*/

	#endregion

	#endregion

	#region Control

	public virtual void SetAutomaticSteps()
	{
		IsAutomaticStepingEnabled = !IsAutomaticStepingEnabled;

		if ( IsAutomaticStepingEnabled )
		{
			Chrono.Start();
		}
		else
		{
			Chrono.Stop();
		}
	}

	public virtual void ResetWorld()
	{
		DestroyWorld();

		MathFunctions.ResetStatistics();

		CreateWorld();

		Chrono.Reset();
	}

	public virtual void DestroyWorld()
	{
		Chrono.Stop();

		if ( World != null )
		{
			foreach ( WorldCell cell in World )
			{
				cell.DestroyWorldCell();
			}

			Destroy( RenderParent );
		}
	}

	public virtual string GetStatus()
	{
		return $"Time elapsed : { Chrono.Elapsed } \n{ MathFunctions.GetStatistics() }";
	}

	public virtual void ResetStatistics()
	{
		MathFunctions.ResetStatistics();
	}

	#endregion

	#region Methods for extern use	

	public virtual bool IsPositionInWorld( Vector2Int position )
	{
		bool isIn = true;

		if ( position.x >= Xsize || position.x < 0 )
		{
			isIn = false;
		}
		else if ( position.y >= Zsize || position.y < 0 )
		{
			isIn = false;
		}

		return isIn;
	}

	public virtual bool IsPositionAccesible( Vector2Int position )
	{
		return IsPositionInWorld( position ) && World[position.x, position.y].IsTreadmillable;
	}

	public virtual bool MoveToCell( WorldObject obj, Vector2Int target )
	{
		bool moved = false;

		if ( IsPositionAccesible( target ) )
		{
			WorldCell originCell = World[obj.WorldPosition2D.x, obj.WorldPosition2D.y];
			WorldCell targetCell = World[target.x, target.y];

			originCell.Content = null;
			targetCell.Content = obj;

			moved = true;
		}
		else
		{
			UnityEngine.Debug.LogWarning( $"Impossible move element {obj} to target {target}" );
		}

		return moved;
	}

	public virtual Vector3 WorldToRealPosition( Vector3Int worldPosition3D )
	{
		return transform.position + new Vector3( worldPosition3D.x * CellXsize, worldPosition3D.y * CellYsize, worldPosition3D.z * CellZsize );
	}

	public virtual WorldObject GetCellContent( Vector2Int position )
	{
		WorldObject result = null;

		if ( IsPositionInWorld( position ) )
		{
			result = World[position.x, position.y].Content;
		}

		return result;
	}

	public virtual void SetCellContent( Vector2Int position, WorldObject obj )
	{
		if ( IsPositionAccesible( position ) )
		{
			World[position.x, position.y].Content = obj;
		}
	}

	public virtual void DestroyedEntity( Entity objectDestroyed )
	{
		//UnityEngine.Debug.Log( $"The entity {objectDestroyed} has dead. Time = {Chrono.Elapsed}" );
		// TODO : Implement statistics for this
	}

	public Vector3 GetIniCellPos()
	{
		return WorldToRealPosition( World[0, 0].WorldPosition3D );
	}

	#endregion

	#region Auxiliar

	protected virtual float InverseLerpHeight( float height, WorldCellType type )
	{
		return Mathf.InverseLerp( MinAndMaxHeights.x, MinAndMaxHeights.y, height );
	}

	#endregion
}
