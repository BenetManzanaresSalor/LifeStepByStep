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
	[SerializeField] protected Material RenderMaterial;


	#endregion

	#region Function attributes

	protected int ChunkSize;
	protected GameObject[,] Chunks;
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

	protected abstract void CreateCellMesh( int x, int z, LC_Chunk chunk, Cell[,] cells );

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

	#region Auxiliar

	public virtual Vector3 TerrainPosToReal( Vector3Int pos )
	{
		return transform.position + new Vector3( pos.x * CellSize.x, pos.y * CellSize.y, pos.z * CellSize.z );
	}

	protected virtual Cell GetChunkCell( Vector2Int pos, Cell[,] cells )
	{
		bool isIn = pos.x >= 0 && pos.x < ChunkSize && pos.y >= 0 && pos.y < ChunkSize;
		return isIn ? cells[pos.x, pos.y] : null;
	}

	#endregion
}
