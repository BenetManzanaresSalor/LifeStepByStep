using UnityEngine;

public class WorldObject : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "World Object settings" )]
	[SerializeField] protected float VerticalOffset;
	[SerializeField] protected float RotationOffset;

	#endregion

	#region Function attributes

	protected World CurrentWorld;
	protected WorldTerrain CurrentTerrain { get => CurrentWorld.Terrain; }
	public WorldCell CurrentCell
	{
		get { return currentCell; }
		set
		{
			if ( currentCell == null )
			{
				currentCell = value;
				transform.position = CurrentPositionToReal();
			}
			else if ( value != currentCell )
			{
				currentCell.DeleteContent();
				CellChange( value );
				currentCell = value;
			}
		}
	}
	protected WorldCell currentCell;
	public Vector2Int WorldPosition2D { get { return CurrentCell.TerrainPos; } }

	#endregion

	#region Initialization

	public virtual void SetWorld( World world )
	{
		CurrentWorld = world;
	}

	#endregion

	#endregion

	#region Movement methods

	protected virtual Vector3 CurrentPositionToReal()
	{
		return CurrentTerrain.TerrainPosToReal( CurrentCell ) + Vector3.up * ( transform.lossyScale.y / 2 + VerticalOffset );
	}

	protected virtual void CellChange( WorldCell newCell )
	{
		transform.position = CurrentPositionToReal();
	}

	#endregion

	#region Destroy

	public virtual void Destroy()
	{
		CurrentCell.DeleteContent();
		Destroy( gameObject );
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"World object in world position {CurrentCell.TerrainPos}";
	}

	#endregion
}