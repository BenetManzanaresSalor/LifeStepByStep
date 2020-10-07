using System;
using System.Collections.Generic;
using UnityEngine;

public struct EntityAction
{
	public Func<bool> Conditions;
	public Func<float> Method;

	public EntityAction( Func<bool> conditions, Func<float> method )
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
	protected const float NotActionCost = NullEnergyCost - 1;

	#endregion

	#region Attributes

	#region Settings

	[Header( "Entity settings" )]
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float Move1msCost = 1;
	[SerializeField] protected Vector2 MinAndMaxMoveSeconds = new Vector2( 0.75f, 1.25f );
	[SerializeField] protected float FastMoveDivisor = 1.5f;
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float SearchCost = NotActionCost;
	[SerializeField] protected uint SearchRadius = 5;
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float PathToTargetCost = NullEnergyCost;
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float RandomMoveCost = NullEnergyCost;
	[SerializeField] [Range( 0, 100 )] protected int ConserveDirectionProbability = 50;

	#endregion

	#region Function attributes

	public bool IsAlive { get; protected set; }
	protected float Energy;
	protected float MovementProgress;
	protected float NormalMoveSeconds;
	protected float FastMoveSeconds;
	protected Vector3 OriginPosition;
	protected Vector3 DestinyPosition { get { return CurrentPositionToReal(); } }
	public Vector2Int Direction
	{
		get { return direction; }
		protected set
		{
			direction = value;
			transform.rotation = Quaternion.Euler( 0, RotationOffset, 0 );

			if ( direction != Vector2Int.zero )
				transform.rotation *= Quaternion.LookRotation( new Vector3( direction.x, 0, direction.y ) );
		}
	}
	protected Vector2Int direction;

	protected List<EntityAction> ActionsList;
	protected WorldObject Target;
	public bool HasTarget { get { return Target != null; } }

	protected System.Random RandomGenerator;

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		IsAlive = true;
		SetEnergy( MaxEnergyValue );
		Direction = Vector2Int.zero;
		RandomGenerator = new System.Random( WorldPosition2D.GetHashCode() );
		NormalMoveSeconds = MinAndMaxMoveSeconds.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxMoveSeconds.y - MinAndMaxMoveSeconds.x );
		FastMoveSeconds = NormalMoveSeconds / FastMoveDivisor;
		MovementProgress = 0;

		ActionsList = CreateActionsList();
	}

	protected virtual List<EntityAction> CreateActionsList()
	{
		return new List<EntityAction>() {
			new EntityAction( HasToMove, MoveAction ),
			new EntityAction( IsTouchingTarget, TouchingTargetAction ),
			new EntityAction( HasToSearch, SearchAndPathToTargetAction ),
			new EntityAction( () => { return true; }, RandomMovementAction ),
		};
	}

	#endregion

	#region Update step

	public virtual bool Step()
	{
		if ( IsAlive )
			IncrementEnergy( -DoAction() );
		else
			Destroy();

		return IsAlive;
	}

	#endregion

	#region Actions

	public virtual float DoAction()
	{
		float cost = NullEnergyCost;
		bool actionDone = false;
		EntityAction action;

		for ( int i = 0; i < ActionsList.Count && !actionDone; i++ )
		{
			action = ActionsList[i];
			if ( action.Conditions() )
			{
				cost = action.Method();
				actionDone = cost >= NullEnergyCost;
			}
		}

		cost = Mathf.Max( NullEnergyCost, cost );
		return cost;
	}

	#region Move

	protected override void CellChange( WorldCell newCell )
	{
		Direction = newCell.TerrainPos - WorldPosition2D;
		OriginPosition = CurrentPositionToReal();
	}

	protected virtual bool HasToMove()
	{
		return transform.position != DestinyPosition;
	}

	protected virtual float MoveAction()
	{
		float cost = NullEnergyCost;

		float currentMovementSeconds = HasTarget ? FastMoveSeconds : NormalMoveSeconds;
		MovementProgress = Mathf.Min( MovementProgress + Time.deltaTime / currentMovementSeconds, 1 );
		if ( MovementProgress != 1 )
		{
			transform.position = new Vector3(
			Mathf.Lerp( OriginPosition.x, DestinyPosition.x, MovementProgress ),
			Mathf.Lerp( OriginPosition.y, DestinyPosition.y, MovementProgress ),
			Mathf.Lerp( OriginPosition.z, DestinyPosition.z, MovementProgress ) );
		}
		else
		{
			transform.position = DestinyPosition;
			MovementProgress = 0;
			cost = EndedMovement();
		}

		return cost;
	}

	protected virtual float EndedMovement()
	{
		float currentMovementSeconds = HasTarget ? FastMoveSeconds : NormalMoveSeconds;
		float speed = 1 / currentMovementSeconds;
		return speed * Move1msCost;
	}

	#endregion

	#region Touching target

	protected virtual bool IsTouchingTarget()
	{
		return Target != null && MathFunctions.IsTouchingObjective( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible );
	}

	protected abstract float TouchingTargetAction();

	#endregion

	#region Search and move to target

	protected abstract bool HasToSearch();

	protected virtual float SearchAndPathToTargetAction()
	{
		float cost = SearchCost;
		List<WorldObject> interestingObjs = new List<WorldObject>();
		WorldObject closestInterestingObj = null;
		float closestInterestingObjDistance = float.MaxValue;

		WorldObject currentWorldObject;
		float currentDistance;
		foreach ( Vector2Int position in MathFunctions.AroundPositions( WorldPosition2D, SearchRadius ) )
		{
			currentWorldObject = CurrentTerrain.GetCellContent( position ); // TODO ? : Make possible search WorldCell

			if ( IsInterestingObject( currentWorldObject ) )
			{
				interestingObjs.Add( currentWorldObject );

				currentDistance = position.Distance( WorldPosition2D );
				if ( currentDistance < closestInterestingObjDistance )
				{
					closestInterestingObjDistance = currentDistance;
					closestInterestingObj = currentWorldObject;
				}
			}
		}

		Target = TargetAtInterestingObjects( closestInterestingObj, interestingObjs );
		if ( Target != null )
			if ( cost < NullEnergyCost )
				cost = TryPathToTarget();
			else
				cost += TryPathToTarget();

		return cost;
	}

	protected abstract bool IsInterestingObject( WorldObject obj );

	protected virtual WorldObject TargetAtInterestingObjects( WorldObject closestInterestingObj, List<WorldObject> interestingObjs )
	{
		return closestInterestingObj;
	}

	protected virtual float TryPathToTarget()
	{
		float cost = NotActionCost;
		bool movementDone = false;

		List<Vector2Int> PathToTarget = MathFunctions.Pathfinding( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible, (int)SearchRadius * 2 );

		// If a path to target is possible
		if ( PathToTarget.Count > 0 && MathFunctions.IsTouchingObjective( PathToTarget[PathToTarget.Count - 1], Target.WorldPosition2D, CurrentTerrain.IsPosAccesible ) )
		{
			// Try movement
			movementDone = CurrentTerrain.TryMoveToCell( this, PathToTarget[0] );
			if ( movementDone )
			{
				cost = PathToTargetCost;
				PathToTarget.RemoveAt( 0 );
			}
		}

		return cost;
	}

	#endregion

	#region Random movement

	protected virtual float RandomMovementAction()
	{
		bool movementDone = false;
		Vector2Int nextDirection;

		nextDirection = MathFunctions.PseudorandomDirection( WorldPosition2D, Direction, RandomGenerator, ConserveDirectionProbability, CurrentTerrain.IsPosAccesible );

		// If exists some possible movement
		if ( !nextDirection.Equals( Vector2Int.zero ) )
			movementDone = CurrentTerrain.TryMoveToCell( this, WorldPosition2D + nextDirection );

		return movementDone ? RandomMoveCost : NotActionCost;
	}

	#endregion

	#endregion

	#region Energy and destruction

	protected virtual void SetEnergy( float value )
	{
		Energy = Mathf.Clamp( value, MinEnergyValue, MaxEnergyValue );
		if ( Energy <= MinEnergyValue )
			Destroy();
	}

	protected virtual void IncrementEnergy( float amount )
	{
		SetEnergy( Energy + amount );
	}

	public override void Destroy()
	{
		IsAlive = false;
		CurrentWorld.DestroyedEntity( this );
		base.Destroy();
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"Entity in world position {WorldPosition2D}";
	}

	#endregion
}