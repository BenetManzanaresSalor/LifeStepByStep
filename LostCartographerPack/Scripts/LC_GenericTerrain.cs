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
	[SerializeField] [Range( 0, 64 )] protected int ChunkRenderDistance = 4;
	[SerializeField] protected Transform Player;
	[SerializeField] protected Material RenderMaterial;

	#endregion

	#region Function attributes

	protected int ChunkSize;
	protected Vector2Int PlayerChunkPos;
	protected float ChunkRenderRealDistance;
	protected Dictionary<Vector2Int, LC_Chunk> LoadedChunks;
	protected int MaxVerticesPerRenderElem = 12;
	protected List<Vector3> vertices;
	protected List<int> triangles;
	protected List<Vector2> uvs;

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		ChunkSize = (int)Mathf.Pow( 2, ChunkSizeLevel );

		ChunkRenderRealDistance = ChunkRenderDistance * ChunkSize * Mathf.Max( CellSize.x, CellSize.z );
		LoadedChunks = new Dictionary<Vector2Int, LC_Chunk>();

		PlayerChunkPos = RealPosToChunk( Player.position );

		IniTerrain();
	}

	protected virtual void IniTerrain()
	{
		CreateChunk( PlayerChunkPos );

		foreach ( Vector2Int pos in MathFunctions.AroundPositions( Vector2Int.zero, (uint)ChunkRenderDistance ) )
		{
			CreateChunk( pos + PlayerChunkPos );
		}
	}

	protected virtual void CreateChunk( Vector2Int chunkPos )
	{
		LC_Chunk chunk = new LC_Chunk( new GameObject(), chunkPos * ChunkSize );
		chunk.Obj.transform.parent = this.transform;
		chunk.Obj.name = "Chunk_" + chunkPos;
		chunk.Obj.transform.position = TerrainPosToReal( new Vector3Int( chunk.CellsOffset.x, 0, chunk.CellsOffset.y ) );

		Cell[,] cells = CreateCells( chunk );
		CreateMesh( chunk, cells );

		LoadedChunks.Add( chunkPos, chunk );
	}

	protected virtual Cell[,] CreateCells( LC_Chunk chunk )
	{
		Cell[,] cells = new Cell[ChunkSize + 1, ChunkSize + 1];	// +1 for edges
		for ( int x = 0; x < cells.GetLength( 0 ); x++ )
		{
			for ( int z = 0; z < cells.GetLength( 1 ); z++ )
			{
				cells[x, z] = CreateChunkCell( x, z, chunk );
			}
		}

		return cells;
	}

	public abstract Cell CreateChunkCell( int chunkX, int chunkZ, LC_Chunk chunk );

	protected virtual void CreateMesh( LC_Chunk chunk, Cell[,] cells )
	{
		// Initialize render lists
		vertices = new List<Vector3>();
		triangles = new List<int>();
		uvs = new List<Vector2>();

		// Render all elements
		CellsToMesh( chunk, cells );

		// Create mesh if some vertices remains
		if ( vertices.Count > 0 )
		{
			CreateMeshObj( chunk.Obj );
		}
	}

	#endregion

	#region Render

	protected virtual void CellsToMesh( LC_Chunk chunk, Cell[,] cells )
	{
		for ( int x = 0; x < cells.GetLength( 0 ); x++ )
		{
			for ( int z = 0; z < cells.GetLength( 1 ); z++ )
			{
				CreateCellMesh( x, z, chunk, cells );

				// Create mesh before get the maximum mesh vertices at next cell render
				if ( vertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
				{
					CreateMeshObj( chunk.Obj );
				}
			}
		}
	}

	protected abstract void CreateCellMesh( int chunkX, int chunkZ, LC_Chunk chunk, Cell[,] cells );

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

	#endregion

	#region Dynamic chunk loading

	protected virtual void Update()
	{
		UpdateChunks();
	}

	protected virtual void UpdateChunks()
	{
		Vector2Int newPlayerChunkPos = RealPosToChunk( Player.position );
		Vector2Int offset = newPlayerChunkPos - PlayerChunkPos;

		// If chunk pos changed
		if ( offset.magnitude > 0 )
		{
			PlayerChunkPos = newPlayerChunkPos;

			List<Vector2Int> aroundChunksPos = MathFunctions.AroundPositions( newPlayerChunkPos, (uint)ChunkRenderDistance );
			aroundChunksPos.Add( newPlayerChunkPos );
			List<Vector2Int> chunksToUnload = new List<Vector2Int>();

			LC_Chunk chunk;
			Vector2Int chunkPos;
			int index;
			foreach ( KeyValuePair<Vector2Int, LC_Chunk> entry in LoadedChunks )
			{
				chunkPos = entry.Key;
				chunk = entry.Value;
				index = aroundChunksPos.IndexOf( chunkPos );
				if ( index >= 0 )
				{
					aroundChunksPos.RemoveAt( index );
				}
				else
				{
					chunksToUnload.Add( chunkPos );
					Destroy( chunk.Obj );
				}
			}

			foreach ( Vector2Int pos in aroundChunksPos )
			{
				CreateChunk( pos );
			}

			foreach ( Vector2Int pos in chunksToUnload )
			{
				LoadedChunks.Remove( pos );
			}
		}
	}

	#endregion

	#region Auxiliar

	public virtual Vector3 TerrainPosToReal( Vector3Int pos )
	{
		return transform.position + new Vector3( pos.x * CellSize.x, pos.y * CellSize.y, pos.z * CellSize.z );
	}

	public virtual Vector3Int RealPosToTerrain( Vector3 pos )
	{
		return new Vector3Int( (int)( pos.x / CellSize.x ), (int)( pos.y / CellSize.y ), (int)( pos.z / CellSize.z ) );
	}

	public virtual Vector2Int RealPosToChunk( Vector3 pos )
	{
		Vector3Int terrainPos = RealPosToTerrain( pos );

		Vector2Int res = new Vector2Int( terrainPos.x / ChunkSize, terrainPos.z / ChunkSize );

		if ( terrainPos.x < 0 )
			res.x -= 1;
		if ( terrainPos.z < 0 )
			res.y -= 1;

		return res;
	}

	#endregion
}
