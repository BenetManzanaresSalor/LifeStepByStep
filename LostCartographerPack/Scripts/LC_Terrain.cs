using UnityEngine;

public class LC_Terrain : LC_GenericTerrain<LC_Cell>
{
	#region Attributes

	#region Settings	

	[Header( "Random generation settings" )]
	[SerializeField] [Range( 1, 128 )] protected int HeightsMapDivisor = 1;
	[SerializeField] protected Vector2Int MinAndMaxHeights = new Vector2Int( 0, 1 );
	[SerializeField] protected bool RandomMapSeed = true;
	[SerializeField] protected int MapSeed;
	[SerializeField] [Range( 0, 64 )] protected int Octaves = 4;
	[SerializeField] [Range( 0, 1 )] protected float Persistance = 0.5f;
	[SerializeField] [Range( 0, 1 )] protected float Lacunarity = 0.2f;

	[Header( "Additional render settings" )]
	[SerializeField] protected Vector2Int TextureColumnsAndRows = Vector2Int.one;
	[SerializeField] [Range( 1, 4 )] protected float TextureMarginRelation = 3;

	#endregion

	#region Function attributes

	protected System.Random RandomGenerator;
	protected float[,] HeightsMap;

	protected Vector2 TextureSize;
	protected Vector2 TextureMargin;

	#endregion

	#endregion

	#region Initialization

	protected override void Start()
	{
		TextureSize = new Vector2( 1f / TextureColumnsAndRows.x, 1f / TextureColumnsAndRows.y );
		TextureMargin = TextureSize / TextureMarginRelation;

		RandomGenerator = new System.Random();
		if ( RandomMapSeed ) MapSeed = RandomGenerator.Next();

		base.Start();
	}

	protected override void CreateChunk( Vector2Int chunkPos )
	{
		CreateChunkHeightsMap( chunkPos );
		base.CreateChunk( chunkPos );
	}

	protected virtual void CreateChunkHeightsMap( Vector2Int chunkPos )
	{
		HeightsMap = MathFunctions.PerlinNoiseMap(
			new Vector2Int( ChunkSize + 1, ChunkSize + 1 ), // +1 for edges
			MapSeed,
			Octaves, Persistance, Lacunarity,
			MinAndMaxHeights,
			HeightsMapDivisor,
			chunkPos.x * ChunkSize,
			chunkPos.y * ChunkSize,
			true );
	}

	public override LC_Cell CreateChunkCell( int chunkX, int chunkZ, LC_Chunk chunk )
	{
		int height = Mathf.RoundToInt( HeightsMap[chunkX, chunkZ] );
		return new LC_Cell( new Vector3Int( chunk.CellsOffset.x + chunkX, height, chunk.CellsOffset.y + chunkZ ) );
	}

	#endregion

	#region Render

	protected override void CreateCellMesh( int chunkX, int chunkZ, LC_Chunk chunk, LC_Cell[,] cells )
	{
		LC_Cell cell = cells[chunkX, chunkZ];
		Vector3 realPos = TerrainPosToReal( cell.TerrainPos );

		// Vertices
		vertices.Add( realPos );

		// Triangles
		if ( chunkX < cells.GetLength( 0 ) - 1 && chunkZ < cells.GetLength( 1 ) - 1 )
		{
			int vertexI = vertices.Count - 1;

			triangles.Add( vertexI );
			triangles.Add( vertexI + 1 );
			triangles.Add( vertexI + cells.GetLength( 0 ) + 1 );

			triangles.Add( vertexI );
			triangles.Add( vertexI + cells.GetLength( 0 ) + 1 );
			triangles.Add( vertexI + cells.GetLength( 0 ) );
		}

		// UVs
		GetUVs( new Vector2Int( chunkX, chunkZ ), out Vector2 iniUV, out Vector2 endUV, chunk, cells );
		uvs.Add( iniUV );
	}

	public void GetUVs( Vector2Int chunkPos, out Vector2 ini, out Vector2 end, LC_Chunk chunk, LC_Cell[,] cells )
	{
		Vector2Int texPos = GetTexPos( cells[chunkPos.x, chunkPos.y], chunk, cells );

		end = new Vector2( ( texPos.x + 1f ) / TextureColumnsAndRows.x, ( texPos.y + 1f ) / TextureColumnsAndRows.y ) - TextureMargin;
		ini = end - TextureMargin;
	}

	protected virtual Vector2Int GetTexPos( LC_Cell cell, LC_Chunk chunk, LC_Cell[,] cells )
	{
		float value = Mathf.InverseLerp( MinAndMaxHeights.x, MinAndMaxHeights.y, cell.TerrainPos.y );
		int y = (int)Mathf.Clamp( TextureColumnsAndRows.y * value, 0, TextureColumnsAndRows.y - 1 );

		return new Vector2Int( 0, y );
	}

	#endregion
}
