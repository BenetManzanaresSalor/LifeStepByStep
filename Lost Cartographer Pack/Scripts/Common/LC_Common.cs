using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Default cell class of the Lost Cartographer Pack.
/// </summary>
public class LC_Cell
{
	public Vector2Int TerrainPos;
	public float Height;

	public LC_Cell( Vector2Int terrainPosition, float height )
	{
		TerrainPos = terrainPosition;
		Height = height;
	}
}

/// <summary>
/// Default chunk class of the Lost Cartographer Pack.
/// </summary>
public class LC_Chunk<Cell> where Cell : LC_Cell
{
	public GameObject Obj;
	public Vector2Int Position;
	public Vector2Int CellsOffset;
	public float[,] HeightsMap;
	public Cell[,] Cells;
	public Task ParallelTask;

	public List<Vector3> Vertices;
	public List<int> Triangles;
	public List<Vector2> UVs;
	public Vector3[] Normals;
	public Vector3[] VerticesArray;
	public int[] TrianglesArray;
	public Vector2[] UVsArray;

	public LC_Chunk( Vector2Int position, int chunkSize )
	{
		Position = position;
		CellsOffset = Position * chunkSize;

		Vertices = new List<Vector3>();
		Triangles = new List<int>();
		UVs = new List<Vector2>();
		Normals = null;
	}

	public Vector2Int TerrainPosToChunk( Vector2Int cellTerrainPos )
	{
		return cellTerrainPos - CellsOffset;
	}

	public Vector2Int ChunkPosToTerrain( Vector2Int cellChunkPos )
	{
		return cellChunkPos + CellsOffset;
	}

	public void BuildMesh()
	{
		// Convert to array
		VerticesArray = Vertices.ToArray();
		TrianglesArray = Triangles.ToArray();
		UVsArray = UVs.ToArray();

		// Clear lists
		Vertices.Clear();
		Triangles.Clear();
		UVs.Clear();
	}

	public void Destroy()
	{
		if ( Obj != null )
		{
			MeshFilter meshFilter = Obj.GetComponent<MeshFilter>();
			if ( meshFilter )
				UnityEngine.Object.Destroy( meshFilter.sharedMesh );

			MeshCollider meshCollider = Obj.GetComponent<MeshCollider>();
			if ( meshCollider )
				UnityEngine.Object.Destroy( meshCollider.sharedMesh );

			UnityEngine.Object.Destroy( Obj );
		}

		if ( ParallelTask != null && !ParallelTask.IsCompleted && !ParallelTask.IsCanceled && !ParallelTask.IsFaulted )
			ParallelTask.Dispose();

		HeightsMap = null;
		Cells = null;

		Vertices?.Clear();
		Triangles?.Clear();
		UVs?.Clear();
		Normals = null;

		VerticesArray = null;
		TrianglesArray = null;
		UVsArray = null;
	}
}