using UnityEngine;

[RequireComponent( typeof( WorldTerrain ) )]
public abstract class GenericWorld : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "World global settings" )]
	[SerializeField] protected WorldObject[] WorldObjects;

	#endregion

	#region Function attributes

	public WorldTerrain Terrain { get; protected set; }
	public bool AutomaticSteping { get; protected set; }

	#endregion

	#endregion

	#region Initialization

	public virtual void Generate()
	{
		if ( Terrain == null )
			Terrain = GetComponent<WorldTerrain>();

		Terrain.Generate();
	}

	public virtual WorldCell CreateCell( int chunkX, int chunkZ, LC_Chunk<WorldCell> chunk )
	{
		WorldCell cell = new WorldCell( new Vector2Int( chunk.CellsOffset.x + chunkX, chunk.CellsOffset.y + chunkZ ),
			chunk.HeightsMap[chunkX + 1, chunkZ + 1] ); // +1 to compensate the offset for normals computation
		cell.Height = Mathf.RoundToInt( cell.Height );
		return cell;
	}

	public abstract WorldObject CreateWorldObject( WorldCell cell );

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
		UnityEngine.Debug.Log( $"{entity} DESTROYED" );
	}

	#endregion
}