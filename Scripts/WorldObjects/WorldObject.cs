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

	protected GenericWorld CurrentWorld;
	protected WorldTerrain CurrentTerrain { get => CurrentWorld.Terrain; }
	public WorldCell CurrentCell
	{
		get { return currentCell; }
		set
		{
			if ( currentCell == null )
			{
				currentCell = value;
				transform.position = WorldPositionToReal();
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

	protected virtual void Start()
	{
		// TODO : Create own mesh
	}

	public virtual void SetWorld( GenericWorld world )
	{
		CurrentWorld = world;
	}

	#endregion

	#endregion

	#region Movement methods

	protected virtual Vector3 WorldPositionToReal()
	{
		return CurrentTerrain.TerrainPosToReal( CurrentCell ) + Vector3.up * ( transform.lossyScale.y / 2 + VerticalOffset );
	}

	protected virtual void CellChange( WorldCell newCell )
	{
		transform.position = WorldPositionToReal();
	}

	#endregion

	#region Destroy

	public virtual void DestroyWorldObject()
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