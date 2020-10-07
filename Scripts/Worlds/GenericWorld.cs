using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof( WorldTerrain ) )]
public abstract class GenericWorld : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "World global settings" )]
	[SerializeField] protected Animal[] Animals;
	[SerializeField] protected Food[] Foods;
	[SerializeField] protected WorldObject[] Obstacles;
	[SerializeField] protected float MaxUpdateTime = 1f / ( 60f * 2f );

	#endregion

	#region Function attributes

	public WorldTerrain Terrain { get; protected set; }
	protected List<Entity> EntitiesList;
	public bool AutomaticSteping { get; protected set; }
	public float UpdateIniTime { get; protected set; }
	protected int EntityIdx = 0;

	#endregion

	#endregion

	#region Initialization

	public virtual void Generate()
	{
		if ( Terrain == null )
			Terrain = GetComponent<WorldTerrain>();

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

	public abstract WorldObject GetWorldObject( WorldCell cell );

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
					EntityIdx = ( EntityIdx - 1 ) % EntitiesList.Count;	// Adjust because of remove
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