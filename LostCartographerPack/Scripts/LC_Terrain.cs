using UnityEngine;

public class LC_Terrain : LC_GenericTerrain<LC_Cell>
{
	#region Attributes

	#region Settings	

	[Header( "Random generation settings" )]
	[SerializeField] [Range( 1, 8 )] protected int TerrainSizeLevel = 4;
	[SerializeField] [Range( 1, 128 )] protected int MapDivisor = 1;
	[SerializeField] protected Vector2Int MinAndMaxHeights = new Vector2Int( 0, 1 );
	[SerializeField] protected bool RandomMapSeed = true;
	[SerializeField] protected float MapSeed;
	[SerializeField] protected int Octaves = 4;
	[SerializeField] protected float Persistance = 0.5f;
	[SerializeField] protected float Lacunarity = 0.2f;

	[Header( "Additional render settings" )]
	[SerializeField] protected LC_RenderType RendererType = LC_RenderType.HEIGHT;
	[SerializeField] [Range( 1, 10 )] protected int SmoothingSize = 2;
	[SerializeField] protected Vector2Int TextureColumnsAndRows = Vector2Int.one;
	[SerializeField] [Range( 1, 4 )] protected float TextureMarginRelation = 3;

	#endregion

	#region Function attributes

	protected int TerrainSize;
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

		TerrainSize = (int)Mathf.Pow( 2, TerrainSizeLevel );
		RandomGenerator = new System.Random();
		CreateMap();

		base.Start();
	}

	protected virtual void CreateMap()
	{
		if ( RandomMapSeed ) MapSeed = (float)RandomGenerator.NextDouble() * 100f;

		HeightsMap = MathFunctions.PerlinNoiseMap(
			TerrainSize / MapDivisor,
			TerrainSize / MapDivisor,
			MapSeed,
			Octaves, Persistance, Lacunarity,
			MinAndMaxHeights.x, MinAndMaxHeights.y );
	}

	public override LC_Cell CreateCell( int x, int z )
	{
		int height = Mathf.RoundToInt(
				MathFunctions.ScaleUpMatrixValue(
					( a, b ) => HeightsMap[a, b],
					MapDivisor,
					( x < 0 ? -x : x ) % TerrainSize,
					( z < 0 ? -z : z ) % TerrainSize,
					new Vector2Int( HeightsMap.GetLength( 0 ), HeightsMap.GetLength( 1 ) ),
					( a, b ) => a * b,
					( a, b ) => a + b ) );

		return new LC_Cell( new Vector3Int( x, height, z ) );
	}

	#endregion

	#region Render

	protected override void CreateCellMesh( int x, int z, LC_Chunk chunk, LC_Cell[,] cells )
	{		
		LC_Cell cell = cells[x, z];
		Vector3 realPos = TerrainPosToReal( cell.TerrainPos );

		// Vertices
		vertices.Add( realPos );

		// Triangles
		if ( x < ChunkSize - 1 && z < ChunkSize - 1 )
		{
			int vertexI = vertices.Count - 1;

			triangles.Add( vertexI );
			triangles.Add( vertexI + 1 );
			triangles.Add( vertexI + ChunkSize + 1 );

			triangles.Add( vertexI );
			triangles.Add( vertexI + ChunkSize + 1 );
			triangles.Add( vertexI + ChunkSize );
		}

		// UVs
		GetUVs( new Vector2Int(x,z), out Vector2 iniUV, out Vector2 endUV, chunk, cells );
		uvs.Add( iniUV );
	}

	public void GetUVs( Vector2Int pos, out Vector2 ini, out Vector2 end, LC_Chunk chunk, LC_Cell[,] cells )
	{
		Vector2Int texPos = GetTexPos( cells[pos.x, pos.y], chunk, cells );

		end = new Vector2( ( texPos.x + 1f ) / TextureColumnsAndRows.x, ( texPos.y + 1f ) / TextureColumnsAndRows.y ) - TextureMargin;
		ini = end - TextureMargin;
	}

	protected virtual Vector2Int GetTexPos( LC_Cell cell, LC_Chunk chunk, LC_Cell[,] cells )
	{
		float value;

		switch ( RendererType )
		{
			case LC_RenderType.SMOOTHING:
				value = GetSmoothingRenderValue( cell, chunk, cells );
				break;
			case LC_RenderType.HEIGHT:
				value = GetHeightRenderValue( cell );
				break;
			default:
				value = 0;
				break;
		}

		int y = (int)Mathf.Clamp( TextureColumnsAndRows.y * value, 0, TextureColumnsAndRows.y - 1 );

		return new Vector2Int( 0, y );
	}

	protected virtual float GetHeightRenderValue( LC_Cell cell )
	{
		return Mathf.InverseLerp( MinAndMaxHeights.x, MinAndMaxHeights.y, cell.TerrainPos.y );
	}

	protected virtual float GetSmoothingRenderValue( LC_Cell cell, LC_Chunk chunk, LC_Cell[,] cells )
	{
		float value = GetHeightRenderValue( cell );
		Vector2Int cellChunkPos = chunk.CellPosToChunk( cell.TerrainPos );

		int numCells = 1;
		LC_Cell otherCell;
		foreach ( Vector2Int pos in MathFunctions.AroundPositions( cellChunkPos, (uint)SmoothingSize ) )
		{
			otherCell = GetChunkCell( pos, cells );

			if ( otherCell != null )
			{
				value += GetHeightRenderValue( otherCell );
				numCells++;
			}
		}

		return value / numCells;
	}

	#endregion
}
