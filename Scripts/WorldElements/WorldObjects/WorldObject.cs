using UnityEngine;

public class WorldObject : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "World Object settings" )]
	[SerializeField] protected char identificator;
	[SerializeField] protected float VerticalOffset;
	[SerializeField] protected float RotationOffset;

	#endregion

	#region Data accesors

	public char Identificator { get { return identificator; } }

	public WorldMaster World { get { return WorldCell.World; } }
	public WorldCell WorldCell
	{
		get { return worldCell; }
		set
		{
			if ( worldCell == null )
			{
				worldCell = value;
				transform.position = WorldPositionToReal( WorldPosition3D );				
			}
			else if ( value != worldCell )
			{
				WorldPositionMovement( value.WorldPosition3D );
				worldCell = value;
			}
		}
	}
	protected WorldCell worldCell;
	public Vector3Int WorldPosition3D { get { return WorldCell.WorldPosition3D; } }
	public Vector2Int WorldPosition2D { get { return WorldCell.WorldPosition2D; } }

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		// TODO : Create own mesh
	}

	#endregion

	#endregion

	#region Movement methods

	protected virtual void WorldPositionMovement( Vector3Int newWorldPosition3D )
	{
		transform.position = WorldPositionToReal( newWorldPosition3D );
	}

	protected virtual Vector3 WorldPositionToReal( Vector3Int worldPosition3D )
	{
		return World.WorldToRealPosition( worldPosition3D ) + Vector3.up * ( transform.lossyScale.y / 2 + VerticalOffset );
	}

	#endregion

	#region Destroy

	public virtual void DestroyWorldObject()
	{
		WorldCell.Content = null;
		Destroy( gameObject );
		Destroy( this );
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"World object in world position {WorldPosition2D}";
	}

	#endregion
}