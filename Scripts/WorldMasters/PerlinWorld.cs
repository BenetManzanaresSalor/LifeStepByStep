using UnityEngine;

public class PerlinWorld : WorldMaster
{
	#region Attributes

	#region Settings

	[Header( "Perlin world settings" )]
	[SerializeField] protected bool RandomMapSeed = true;
	[SerializeField] protected int MapSeed;
	[SerializeField] [Range( 1, 10 )] protected int MapLevel = 2;
	[SerializeField] protected int WaterLevel = 0;
	[SerializeField] [Range( 0, 100 )] protected float ObjectProbability = 0.5f;
	[SerializeField] [Min( 1 )] protected int MapDivisor = 2;
	[SerializeField] protected int Octaves = 4;
	[SerializeField] protected float Persistance = 0.5f;
	[SerializeField] protected float Lacunarity = 0.2f;

	#endregion

	protected float[,] HeightsMap;
	protected System.Random RandomGenerator;

	#endregion

	#region Initialization

	protected override void Start()
	{
		RandomGenerator = new System.Random();
		base.Start();
	}

	protected override void CreateMap()
	{
		if ( RandomMapSeed ) MapSeed = RandomGenerator.Next();
		int mapSize = (int)Mathf.Pow( 2f, MapLevel );
		Xsize = mapSize;
		Zsize = mapSize;

		Vector2Int size = new Vector2Int( Xsize / MapDivisor, Zsize / MapDivisor );
		HeightsMap = MathFunctions.PerlinNoiseMap( size, MapSeed, Octaves, Persistance, Lacunarity, MinAndMaxHeights );
	}

	protected override WorldCell CreateWorldCell( int x, int z )
	{
		WorldCellType type = DefaultWorldCellType;
		WorldObject content = null;
		Vector3Int worldPosition3D = new Vector3Int( x,
			Mathf.RoundToInt( MathFunctions.ScaleUpMatrixValue(
				( a, b ) => HeightsMap[a, b], MapDivisor, x, z,
				new Vector2Int( HeightsMap.GetLength( 0 ), HeightsMap.GetLength( 1 ) ),
				( a, b ) => a * b,
				( a, b ) => a + b ) ),
			z );

		// If is on water level
		if ( worldPosition3D.y <= WaterLevel )
		{
			type = WorldCellType.WATER;
		}
		// Else is ground
		else
		{
			type = WorldCellType.GROUND;

			if ( RandomGenerator.NextDouble() * 100 < ObjectProbability )
			{
				content = Instantiate( GetRandomWorldObject(), Vector3.zero, Quaternion.identity, transform );
			}
		}

		return new WorldCell( this, worldPosition3D, type, content );
	}

	#endregion

	#region Methods for extern use

	public override Vector3 WorldToRealPosition( Vector3Int worldPosition3D )
	{
		return transform.position + new Vector3( worldPosition3D.x * CellSize.x,
			MathFunctions.Max( worldPosition3D.y, WaterLevel ) * CellSize.y,
			worldPosition3D.z * CellSize.z );
	}

	#endregion

	#region Auxiliar

	protected override float InverseLerpHeight( float height, WorldCellType type )
	{
		float result = 0;

		switch ( type )
		{
			case WorldCellType.GROUND:
				result = Mathf.InverseLerp( WaterLevel + 1, MinAndMaxHeights.y, height );
				break;
			case WorldCellType.WATER:
				result = 1f - Mathf.InverseLerp( MinAndMaxHeights.x, WaterLevel, height );
				break;
		}

		return result;
	}

	protected WorldObject GetRandomWorldObject()
	{
		return WorldObjects[RandomGenerator.Next( 0, WorldObjects.Length )];
	}

	#endregion
}