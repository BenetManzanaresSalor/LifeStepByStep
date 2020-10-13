using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof( WorldTerrain ) )]
public class World : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "World global settings" )]
	[SerializeField] protected Entity[] Entities;
	[SerializeField] protected Food[] Foods;
	[SerializeField] protected WorldObject[] Obstacles;
	[SerializeField] protected float MaxUpdateTime = 1f / ( 60f * 2f );

	[Header( "Random world settings" )]
	[SerializeField] protected bool UseRandomSeed = true;
	[SerializeField] protected int Seed;
	[SerializeField] [Range( 0, 100 )] protected float EntityProbability;
	[SerializeField] [Range( 0, 100 )] protected float FoodProbability;
	[SerializeField] [Range( 0, 100 )] protected float ObstacleProbability;

	#endregion

	#region Function attributes

	public WorldController Controller { get; protected set; }
	public WorldTerrain Terrain { get; protected set; }
	protected List<Entity> EntitiesList;
	public bool AutomaticSteping { get; protected set; }
	public float UpdateIniTime { get; protected set; }
	protected int EntityIdx = 0;

	public bool TargetRays { get => Controller.TargetRays; }

	public System.Random RandomGenerator { get; protected set; }

	#endregion

	#endregion

	#region Initialization

	public virtual void Generate( WorldController worldController )
	{
		Controller = worldController;

		if ( Terrain == null )
			Terrain = GetComponent<WorldTerrain>();
		RandomGenerator = UseRandomSeed ? new System.Random() : new System.Random( Seed );
		Terrain.RandomGenerator = RandomGenerator;

		if ( EntitiesList == null )
			EntitiesList = new List<Entity>();
		else
			DestroyAllEntities();

		Terrain.Generate();
	}

	public virtual WorldCell CreateCell( int chunkX, int chunkZ, LC_Chunk<WorldCell> chunk )
	{
		float realHeight = Mathf.RoundToInt( chunk.HeightsMap[chunkX + 1, chunkZ + 1] ); // +1 to compensate the offset for normals computation
		bool isWater = realHeight <= Terrain.WaterHeight;
		float renderHeight = Mathf.Max( realHeight, Terrain.WaterHeight );
		return new WorldCell( new Vector2Int( chunk.CellsOffset.x + chunkX, chunk.CellsOffset.y + chunkZ ), renderHeight, realHeight, isWater );
	}

	public virtual WorldObject GetWorldObject( WorldCell cell )
	{
		WorldObject worldObj = null;

		int ObjectType = RandomGenerator.Next( 1, 4 );
		switch ( ObjectType )
		{
			case 1:
				if ( EntityProbability > MathFunctions.RandomDouble( RandomGenerator, 0, 100 ) )
					worldObj = Entities[RandomGenerator.Next( 0, Entities.Length )];
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

	public virtual void WorldObjectInstanciated( WorldObject obj )
	{
		Entity entity = obj as Entity;
		if ( entity != null )
			EntitiesList.Add( entity );
	}

	#endregion

	#region Update

	protected virtual void Update()
	{
		if ( AutomaticSteping )
		{
			UpdateIniTime = Time.realtimeSinceStartup;
			float numIterations = 0;
			float averageIterationTime = 0;

			int i;
			bool isAlive;
			for ( i = 0; i < EntitiesList.Count && InMaxUpdateTime( averageIterationTime ); i++ )
			{
				EntityIdx = ( EntityIdx + 1 ) % EntitiesList.Count;
				isAlive = EntitiesList[EntityIdx].Step();
				if ( !isAlive )
				{
					EntitiesList.RemoveAt( EntityIdx );
					EntityIdx = ( EntityIdx - 1 ) % EntitiesList.Count; // Adjust because of remove
				}

				numIterations++;
				averageIterationTime = ( Time.realtimeSinceStartup - UpdateIniTime ) / numIterations;
			}


		}
	}

	protected virtual bool InMaxUpdateTime( float averageIterationTime )
	{
		return ( Time.realtimeSinceStartup - UpdateIniTime + averageIterationTime ) <= MaxUpdateTime;
	}

	#endregion

	#region External use

	public Vector3 GetNearestTerrainRealPos( Vector3 realPos )
	{
		Vector3 res = Vector3.zero;

		WorldCell cell = Terrain.GetCell( realPos );
		if ( cell != null )
			res = Terrain.TerrainPosToReal( cell );

		return res;
	}

	public void ToggleAutomaticSteping()
	{
		AutomaticSteping = !AutomaticSteping;
	}

	public void DestroyedEntity( Entity entity )
	{
		UnityEngine.Debug.Log( $"[DESTROYED] {entity}" );
	}

	public void DestroyAllEntities()
	{
		foreach ( Entity entity in EntitiesList )
			entity.Destroy();

		EntitiesList.Clear();
	}

	#endregion
}