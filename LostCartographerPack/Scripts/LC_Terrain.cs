using UnityEngine;

public class LC_Terrain : LC_GenericTerrain<LC_Cell>
{
	#region Attributes

	#region Settings	

	[Header( "Random generation settings" )]
	[SerializeField] [Range( 1, 256 )] protected int TerrainDimension = 16;
	[SerializeField] [Range( 1, 128 )] protected int MapDivisor;
	[SerializeField] protected Vector2Int MinAndMaxHeights = new Vector2Int( 0, 1 );
	[SerializeField] protected bool RandomMapSeed = true;
	[SerializeField] protected float MapSeed;	
	[SerializeField] protected int Octaves = 4;
	[SerializeField] protected float Persistance = 0.5f;
	[SerializeField] protected float Lacunarity = 0.2f;

	[Header( "Additional render settings" )]
	[SerializeField] protected LC_RenderType RendererType = LC_RenderType.SMOOTHING;
	[SerializeField] [Range( 1, 10 )] protected int SmoothingSize = 2;

	#endregion

	#region Function attributes

	protected System.Random RandomGenerator;
	protected float[,] HeightsMap;

	#endregion

	#endregion

	#region Initialization

	protected override void Start()
	{
		RandomGenerator = new System.Random();
		CreateMap();
		base.Start();
	}

	protected virtual void CreateMap()
	{
		if ( RandomMapSeed ) MapSeed = (float)RandomGenerator.NextDouble() * 100f;

		HeightsMap = MathFunctions.PerlinNoiseMap(
			TerrainDimension / MapDivisor,
			TerrainDimension / MapDivisor,
			MapSeed,
			Octaves, Persistance, Lacunarity,
			MinAndMaxHeights.x, MinAndMaxHeights.y );
	}

	public override LC_Cell CreateCell( int x, int z )
	{
		Vector3Int terrainPosition = new Vector3Int( x,
			Mathf.RoundToInt( MathFunctions.ScaleUpMatrixValue(
				( a, b ) => HeightsMap[a, b], MapDivisor, x, z,
				new Vector2Int( HeightsMap.GetLength( 0 ), HeightsMap.GetLength( 1 ) ),
				( a, b ) => a * b,
				( a, b ) => a + b ) ),
			z );

		return new LC_Cell( terrainPosition );
	}

	#endregion

	#region Render

	protected override Vector2Int CreateTexPos( LC_Cell cell, LC_Chunk chunk, LC_Cell[,] cells )
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
		Vector2Int chunkPos = chunk.CellPosToChunk( cell.TerrainPos );

		int numCells = 1;
		LC_Cell otherCell;
		foreach ( Vector2Int pos in MathFunctions.NearlyPositions( chunkPos, (uint)SmoothingSize ) )
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
