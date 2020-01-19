using System;
using System.Collections.Generic;
using UnityEngine;

public delegate bool Predicate();

public struct EntityAction
{
	public Predicate Conditions;
	public Func<float> Method;

	public EntityAction( Predicate conditions, Func<float> method )
	{
		Conditions = conditions;
		Method = method;
	}
}

[RequireComponent( typeof( MeshFilter ) )]
public abstract class Entity : WorldObject
{
	#region Constants

	protected const float MaxEnergyValue = 100;
	protected const float MinEnergyValue = 0;
	protected const float NullEnergyCost = 0;

	#endregion

	#region Attributes

	#region Settings

	[Header( "Entity settings" )]
	[SerializeField] [Min( NullEnergyCost )] protected float MoveCost;
	[SerializeField] [Min( NullEnergyCost )] protected float MoveToTargetCost;
	[SerializeField] [Min( NullEnergyCost )] protected float RandomMoveCost;
	[SerializeField] protected uint SearchRadius = 1;
	[SerializeField] [Range( 0, 100 )] protected int ConserveDirectionProbability = 50;
	[SerializeField] protected Vector2 MinAndMaxNormalMovementSeconds;
	[SerializeField] protected float FastMovementSecondsDivisor = 2;
	[SerializeField] protected bool IsTargetAccesible = false;

	#endregion

	#region Data accesors

	public bool IsAlive { get; protected set; }
	public Vector2Int Direction
	{
		get { return direction; }
		protected set
		{
			direction = value;
			transform.rotation = Quaternion.Euler( 0, RotationOffset, 0 );

			if ( direction != Vector2Int.zero )
			{
				transform.rotation *= Quaternion.LookRotation( new Vector3( direction.x, 0, direction.y ) );
			}
		}
	}
	protected Vector2Int direction;

	#endregion

	protected List<EntityAction> ActionsList;
	protected float Energy { get; private set; }

	protected WorldObject Target;

	protected Vector3 DestinyPosition { get { return base.WorldPositionToReal( WorldCell.WorldPosition3D ); } }
	protected float MovementProgress;
	protected float NormalMovementSeconds;

	protected System.Random RandomGenerator;

	#endregion

	#region Initialization

	protected override void Start()
	{
		base.Start();
			
		IsAlive = true;
		Energy = MaxEnergyValue;
		Direction = Vector2Int.zero;
		RandomGenerator = new System.Random( WorldPosition2D.GetHashCode() );
		NormalMovementSeconds = MinAndMaxNormalMovementSeconds.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxNormalMovementSeconds.y - MinAndMaxNormalMovementSeconds.x );

		ActionsList = CreateActionsList();
	}

	protected virtual List<EntityAction> CreateActionsList()
	{
		return new List<EntityAction>() {
			new EntityAction( IsTouchingTarget, TouchingTargetAction ),
			new EntityAction( () => { return true; }, SearchAndMoveToTargetAction ),
			new EntityAction( () => { return true; }, RandomMovementAction ),
		};
	}
	
	#endregion

	#region Update

	protected virtual void Update()
	{
		if ( World.IsAutomaticStepingEnabled && IsAlive )
		{
			if ( transform.position != DestinyPosition )
			{
				IncrementEnergy ( -Move() );
			}
			else
			{
				IncrementEnergy( -DoAction() );
			}
		}
	}

	#region Movevement animation

	protected override void WorldPositionMovement( Vector3Int newWorldPosition3D )
	{
		MovementProgress = 0;
		Direction = new Vector2Int( newWorldPosition3D.x - WorldPosition3D.x, newWorldPosition3D.z - WorldPosition3D.z );
	}

	protected virtual float Move()
	{
		Vector3 origin = transform.position;
		float currentMovementSeconds = NormalMovementSeconds;
		float cost = MoveCost;

		if ( Target != null )
		{
			currentMovementSeconds /= FastMovementSecondsDivisor;
			cost *= FastMovementSecondsDivisor;
		}

		MovementProgress += Time.deltaTime / currentMovementSeconds;

		Vector3 destinyPosition = DestinyPosition;
		transform.position = new Vector3(
			Mathf.SmoothStep( transform.position.x, destinyPosition.x, MovementProgress ),
			Mathf.SmoothStep( transform.position.y, destinyPosition.y, MovementProgress ),
			Mathf.SmoothStep( transform.position.z, destinyPosition.z, MovementProgress ) );

		if ( transform.position == DestinyPosition )
		{
			EndedMovement();
		}

		return transform.position != origin ? cost : NullEnergyCost - 1;
	}

	protected virtual void EndedMovement() { }

	#endregion

	#region Actions	

	public virtual float DoAction()
	{
		float cost = NullEnergyCost;
		bool isActing = false;
		EntityAction action;		

		for ( int i = 0; i < ActionsList.Count && !isActing; i++ )
		{
			action = ActionsList[i];
			if ( action.Conditions() )
			{
				cost = action.Method();
				isActing = cost >= NullEnergyCost;
			}
		}

		return cost;
	}

	protected virtual bool IsTouchingTarget()
	{
		bool isTouching = false;

		if ( Target != null )
		{
			isTouching = ( IsTargetAccesible ) ? WorldPosition2D == Target.WorldPosition2D : 
				MathFunctions.IsTouchingTarget( WorldPosition2D, Target.WorldPosition2D, World.IsPositionAccesible );
		}

		return isTouching;
	}

	protected abstract float TouchingTargetAction();

	#region Search and move to target

	protected virtual float SearchAndMoveToTargetAction()
	{
		float cost = NullEnergyCost - 1;
		List<WorldObject> interestingObjects = new List<WorldObject>();
		WorldObject closestInterestingObject = null;
		float closestInterestingObjectDistance = float.MaxValue;

		WorldObject currentWorldObject;
		float currentDistance;

		foreach ( Vector2Int position in MathFunctions.NearlyPositions( WorldPosition2D, SearchRadius ) )
		{
			currentWorldObject = World.GetCellContent( position );	// TODO : Make possible search WorldCell

			if ( IsInterestingObject( currentWorldObject ) )
			{
				interestingObjects.Add( currentWorldObject );

				currentDistance = position.Distance( WorldPosition2D );
				if ( currentDistance < closestInterestingObjectDistance )
				{
					closestInterestingObjectDistance = currentDistance;
					closestInterestingObject = currentWorldObject;
				}
			}
		}

		OnInterestingObjects( closestInterestingObject, interestingObjects );

		if( Target != null )
		{
			cost = MovementToTargetAction();
		}

		return cost;
	}

	protected abstract bool IsInterestingObject( WorldObject obj );

	protected abstract void OnInterestingObjects( WorldObject closestInterestingObject, List<WorldObject> interestingObjects );

	protected virtual float MovementToTargetAction()
	{
		bool movementDone = false;

		List<Vector2Int> PathToTarget = MathFunctions.Pathfinding( WorldPosition2D, Target.WorldPosition2D, World.IsPositionAccesible, (int)SearchRadius * 2 );

		// If a path to target is possible
		if ( PathToTarget.Count > 0 && MathFunctions.IsTouchingTarget( PathToTarget[PathToTarget.Count - 1], Target.WorldPosition2D, World.IsPositionAccesible ) )
		{
			// Try movement
			movementDone = World.MoveToCell( this, PathToTarget[0] );
			if ( movementDone )
			{
				PathToTarget.RemoveAt( 0 );
			}
		}

		return movementDone ? MoveToTargetCost : NullEnergyCost - 1;
	}

	#endregion

	protected virtual float RandomMovementAction()
	{
		bool movement = false;
		Vector2Int nextDirection;

		nextDirection = MathFunctions.PseudorandomDirection( WorldPosition2D, Direction, RandomGenerator, ConserveDirectionProbability, World.IsPositionAccesible );

		// If exists some possible movement
		if ( !nextDirection.Equals( Vector2Int.zero ) )
		{
			movement = World.MoveToCell( this, WorldPosition2D + nextDirection );
		}

		return movement ? RandomMoveCost : NullEnergyCost - 1;
	}

	#endregion

	#endregion

	#region Energy and destroy

	protected virtual void SetEnergy( float value )
	{
		Energy = MathFunctions.Clamp( value, MinEnergyValue, MaxEnergyValue );
		if ( Energy <= MinEnergyValue )
		{
			DestroyWorldObject();
		}
	}

	protected virtual void IncrementEnergy( float amount )
	{
		SetEnergy( Energy + amount );
	}

	public override void DestroyWorldObject()
	{
		IsAlive = false;
		World.DestroyedEntity( this );
		base.DestroyWorldObject();
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"Entity in world position {WorldPosition2D}";
	}

	#endregion
}