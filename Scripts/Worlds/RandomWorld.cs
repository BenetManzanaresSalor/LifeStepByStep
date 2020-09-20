using UnityEngine;

public class RandomWorld : GenericWorld
{
	#region Attributes

	#region Settings

	[Header( "Random world settings" )]
	[SerializeField] protected bool UseRandomSeed = true;
	[SerializeField] protected int Seed;
	[SerializeField] [Range( 0, 100 )] protected float ObjectProbability;

	#endregion

	#region Function attributes

	protected System.Random RandomGenerator;

	#endregion

	#endregion

	#region Initialization

	public override void Generate()
	{
		if ( Terrain == null )
			Terrain = GetComponent<WorldTerrain>();

		RandomGenerator = UseRandomSeed ? new System.Random() : new System.Random( Seed );
		Terrain.RandomGenerator = RandomGenerator;

		Terrain.Generate();
	}

	public override WorldObject CreateWorldObject( WorldCell cell )
	{
		WorldObject worldObj = null;

		if ( RandomGenerator.Next( 1, 100 ) <= ObjectProbability )
			worldObj = WorldObjects[RandomGenerator.Next( 0, WorldObjects.Length )];

		return worldObj;
	}

	#endregion
}
