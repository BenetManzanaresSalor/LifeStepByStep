using UnityEngine;

public class RandomWorld : GenericWorld
{
	#region Attributes

	#region Settings

	[Header( "Random world settings" )]
	[SerializeField] protected bool UseRandomSeed = true;
	[SerializeField] protected int Seed;
	[SerializeField] [Range( 0, 100 )] protected float AnimalProbability;
	[SerializeField] [Range( 0, 100 )] protected float FoodProbability;
	[SerializeField] [Range( 0, 100 )] protected float ObstacleProbability;

	#endregion

	#region Function attributes

	protected System.Random RandomGenerator;

	#endregion

	#endregion

	#region Initialization

	public override void Generate( WorldController worldController )
	{
		if ( Terrain == null )
			Terrain = GetComponent<WorldTerrain>();

		RandomGenerator = UseRandomSeed ? new System.Random() : new System.Random( Seed );
		Terrain.RandomGenerator = RandomGenerator;

		base.Generate( worldController );
	}

	public override WorldObject GetWorldObject( WorldCell cell )
	{
		WorldObject worldObj = null;

		int ObjectType = RandomGenerator.Next( 1, 4 );
		switch ( ObjectType )
		{
			case 1:
				if ( AnimalProbability > MathFunctions.RandomDouble( RandomGenerator, 0, 100 )  )
					worldObj = Animals[RandomGenerator.Next( 0, Animals.Length )];
				break;
			case 2:
				if ( FoodProbability > MathFunctions.RandomDouble( RandomGenerator, 0, 100 ) )
					worldObj = Foods[RandomGenerator.Next( 0, Foods.Length )];
				break;
			case 3:
				if ( ObstacleProbability > MathFunctions.RandomDouble( RandomGenerator, 0, 100 ) )
					worldObj = Obstacles[RandomGenerator.Next( 0, Obstacles.Length )];
				break;
		}

		return worldObj;
	}

	#endregion
}
