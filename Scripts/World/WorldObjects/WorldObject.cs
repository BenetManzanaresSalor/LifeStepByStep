using UnityEngine;

public class WorldObject : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "World Object settings" )]
	[SerializeField] protected GameObject Render;
	[SerializeField] protected float VerticalOffset;
	[SerializeField] protected float RotationOffset;
	[SerializeField] protected GameObject SelectedArrow;

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

	public virtual void Initialize( World world, WorldCell cell )
	{
		CurrentWorld = world;
		CurrentCell = cell;

		SetSelected( false );
	}

	#endregion

	#endregion

	#region Movement methods

	protected virtual Vector3 CurrentPositionToReal()
	{
		return CurrentTerrain.TerrainPosToReal( CurrentCell ) + Vector3.up * ( Render.transform.lossyScale.y / 2 + VerticalOffset );
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

	#region External use

	public override string ToString()
	{
		return $"World object in world position {CurrentCell.TerrainPos}";
	}

	public virtual void SetSelected( bool isSelected )
	{
		if ( SelectedArrow != null )
			SelectedArrow.SetActive( isSelected );
	}

	#endregion
}