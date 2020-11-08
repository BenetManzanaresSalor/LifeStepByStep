using UnityEngine;

public class WorldObject : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected GameObject Render;
	[SerializeField] protected GameObject SelectedArrow;

	#endregion

	#region Functional

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

	#region Movement

	protected virtual Vector3 CurrentPositionToReal()
	{
		return CurrentTerrain.TerrainPosToReal( CurrentCell ) + Vector3.up * ( Render.transform.lossyScale.y / 2 );
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

	public virtual void SetSelected( bool isSelected )
	{
		if ( SelectedArrow != null )
			SelectedArrow.SetActive( isSelected );
	}

	public override string ToString()
	{
		return $"World object in world position {CurrentCell.TerrainPos}";
	}
		
	#endregion
}