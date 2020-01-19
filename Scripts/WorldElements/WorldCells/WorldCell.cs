using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldCell
{
	#region Data accesors

	public WorldMaster World { get; protected set; }

	public Vector3Int WorldPosition3D { get; protected set; }
	public Vector2Int WorldPosition2D { get; protected set; }

	public WorldObject Content
	{
		get { return content; }
		set
		{
			// If content has emptied
			if ( value == null )
			{
				content = value;
			}
			else
			{
				if ( IsTreadmillable )
				{
					content = value;
					content.WorldCell = this;
				}
				else Debug.LogWarning( $"Not possible content {content} set in world cell {WorldPosition2D}" );
			}
		}
	}
	protected WorldObject content;

	public WorldCellType Type;

	public bool IsTreadmillable
	{
		get
		{
			return Type.IsTreadmillable() && Content == null;
		}
	}

	#endregion

	#region Constructors

	public WorldCell( WorldMaster world, Vector3Int worldPosition, WorldCellType type, WorldObject content )
	{
		World = world;
		WorldPosition3D = worldPosition;
		WorldPosition2D = new Vector2Int( WorldPosition3D.x, WorldPosition3D.z );
		Type = type;
		Content = content;
	}

	#endregion

	#region Effectors

	public virtual float ValueByEffectors( Func<Vector2Int, WorldCell> getWorldCell, int range )
	{
		List<WorldCell> effectorsInRange = new List<WorldCell>();
		WorldCell closestEffector = null;
		float closestEffectorDistance = float.MaxValue;

		WorldCell possibleEffector;
		float possibleEffectorDistance;
		foreach ( Vector2Int position in MathFunctions.NearlyPositions( WorldPosition2D, (uint)range ) )
		{
			possibleEffector = getWorldCell( position );
			possibleEffectorDistance = WorldPosition2D.Distance( position );

			if ( possibleEffector != null && IsEffector( possibleEffector ) )
			{
				effectorsInRange.Add( possibleEffector );
				if ( possibleEffectorDistance < closestEffectorDistance )
				{
					closestEffectorDistance = possibleEffectorDistance;
					closestEffector = possibleEffector;
				}
			}
		}

		return OnEffectors( closestEffector, closestEffectorDistance, effectorsInRange, range );
	}

	protected virtual bool IsEffector( WorldCell worldbase )
	{
		return Type != worldbase.Type;
	}

	protected virtual float OnEffectors( WorldCell closestEffector, float closestEffectorDistance, List<WorldCell> effectorsInRange, int range )
	{
		if ( closestEffector == null )
		{
			// Max possible distance
			closestEffectorDistance = range;
		}

		return Mathf.InverseLerp( 1f, range, closestEffectorDistance );
	}

	#endregion

	public virtual void DestroyWorldCell()
	{
		content?.DestroyWorldObject();
	}
}