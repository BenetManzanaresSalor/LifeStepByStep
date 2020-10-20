﻿using System;
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

public class Entity : WorldObject
{
	#region Constants

	protected const float MaxEnergyValue = 100;
	protected const float MinEnergyValue = 0;
	protected const float NullEnergyCost = 0;
	protected const float NotActionCost = NullEnergyCost - 1;

	#endregion

	#region Attributes

	#region Settings

	[Header( "Energy" )]
	[SerializeField] [Range( MinEnergyValue, MaxEnergyValue )] protected float ProblematicEnergyPercentage = 50;

	[Header( "Movement" )]
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float Move1msCost = 2;
	[SerializeField] protected Vector2 MinAndMaxMoveSeconds = new Vector2( 0.75f, 1.25f );
	[SerializeField] protected Vector2 MinAndMaxFastMoveDivisor = new Vector2( 1f, 3f );
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float RandomMoveCost = NullEnergyCost;
	[SerializeField] [Range( 0, 100 )] protected int ConserveDirectionProbability = 50;

	[Header( "Search and target" )]
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float SearchCost = NotActionCost;
	[SerializeField] protected int SearchRadius = 5;
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float PathToTargetCost = NullEnergyCost;
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float EatingCost = 0;

	[Header( "Growing and reproduction" )]
	[SerializeField] protected Vector2 MinAndMaxSecondsToGrow = new Vector2( 20f, 40f );
	[SerializeField] protected Vector3 ChildScale = Vector3.one * 0.6f;
	[SerializeField] protected Vector3 AdultScale = Vector3.one;
	[SerializeField] protected Vector2 MinAndMaxReproductionCooldown = new Vector2( 30f, 60f );
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float ReproductionCost = 10;
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float GiveBirthCost = 25;

	[Header( "Death" )]
	[SerializeField] protected Vector2 MinAndMaxSecondsToLive = new Vector2( 60f, 120f );

	[Header( "Information render" )]
	[SerializeField] protected SpriteRenderer FemenineSprite;
	[SerializeField] protected SpriteRenderer MasculineSprite;
	[SerializeField] protected SpriteRenderer EnergyBar;
	[SerializeField] protected Color GoodEnergyColor = Color.green;
	[SerializeField] protected Color ProblematicEnergyColor = Color.red;
	[SerializeField] protected SpriteRenderer SearchingSprite;
	[SerializeField] protected SpriteRenderer HasTargetSprite;
	[SerializeField] protected SpriteRenderer HasToEatSprite;
	[SerializeField] protected SpriteRenderer HasToReproduceSprite;
	[SerializeField] protected SpriteRenderer IsOldSprite;
	[SerializeField] protected LineRenderer TargetLineRenderer;
	[SerializeField] protected ParticleSystem ReproductionParticles;
	[SerializeField] protected ParticleSystem DeathParticles;

	#endregion

	#region Function attributes

	public bool IsAlive { get; protected set; }
	protected float SecondsAlive = 0;
	protected float Energy;
	protected float MovementProgress;
	protected float NormalMoveSeconds;
	protected float FastMoveDivisor;
	protected float FastMoveSeconds;
	protected Vector3 OriginPosition;
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
	protected bool TargetAccesible = false;

	protected List<EntityAction> ActionsList;
	protected WorldObject Target;

	protected System.Random RandomGenerator;

	protected float SecondsToGrow;
	protected bool IsAdult = false;
	protected bool IsFemale;

	protected float ReproductionCooldown;
	protected float LastReproductionTime;
	protected bool IsPregnant = false;
	protected WorldCell PreviousCell;
	protected float ChildNormalMoveSeconds;
	protected float ChildFastMoveDivisor;

	protected float YoungSeconds;
	protected int LastSecondOld = 0;

	#endregion

	#endregion

	#region Initialization

	public override void Initialize( World world, WorldCell cell )
	{
		base.Initialize( world, cell );

		IsAlive = true;
		SetEnergy( MaxEnergyValue );

		RandomGenerator = CurrentWorld.RandomGenerator;

		Direction = Vector2Int.zero;
		NormalMoveSeconds = MinAndMaxMoveSeconds.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxMoveSeconds.y - MinAndMaxMoveSeconds.x );
		FastMoveDivisor = MinAndMaxFastMoveDivisor.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxFastMoveDivisor.y - MinAndMaxFastMoveDivisor.x );
		FastMoveSeconds = NormalMoveSeconds / FastMoveDivisor;
		MovementProgress = 0;

		IsFemale = RandomGenerator.NextDouble() >= 0.5f;
		FemenineSprite.enabled = IsFemale;
		MasculineSprite.enabled = !IsFemale;

		transform.localScale = ChildScale.Div( transform.parent.lossyScale );
		transform.position = CurrentPositionToReal();

		SecondsToGrow = MinAndMaxSecondsToGrow.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxSecondsToGrow.y - MinAndMaxSecondsToGrow.x );
		ReproductionCooldown = MinAndMaxReproductionCooldown.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxReproductionCooldown.y - MinAndMaxReproductionCooldown.x );

		YoungSeconds = MinAndMaxSecondsToLive.x + (float)RandomGenerator.NextDouble() * ( MinAndMaxSecondsToLive.y - MinAndMaxSecondsToLive.x );

		ActionsList = CreateActionsList();
	}

	public virtual void Initialize( World world, WorldCell cell, float normalMoveSeconds, float fastMoveDivisor )
	{
		Initialize( world, cell );

		NormalMoveSeconds = normalMoveSeconds;
		FastMoveDivisor = fastMoveDivisor;
		FastMoveSeconds = NormalMoveSeconds / FastMoveDivisor;
	}

	#endregion

	#region Update step

	public virtual bool Step()
	{
		if ( IsAlive )
		{
			SecondsAlive += Time.deltaTime;

			// If is old, has a probabiliy of die
			if ( SecondsAlive > YoungSeconds )
			{
				// If is a different second
				if ( LastSecondOld != (int)SecondsAlive )
				{
					float deathProbabilty = SecondsAlive / YoungSeconds - 1;
					if ( deathProbabilty > RandomGenerator.NextDouble() * 2 )
						IsAlive = false;

					LastSecondOld = (int)SecondsAlive;
				}
			}

			// If still alive
			if ( IsAlive )
			{
				IncrementEnergy( -DoAction() );
				UpdateStateRenderer();
			}
		}

		return IsAlive;
	}

	protected virtual void UpdateStateRenderer()
	{
		bool hasTarget = Target != null;
		bool isSearching = !hasTarget && HasToSearch();

		SearchingSprite.enabled = isSearching;

		bool hasToEat = HasProblematicEnergy() || Target is Food;
		HasToEatSprite.enabled = hasToEat;
		HasToReproduceSprite.enabled = !hasToEat && HasToReproduce();

		IsOldSprite.enabled = SecondsAlive > YoungSeconds;

		HasTargetSprite.enabled = hasTarget;

		if ( hasTarget && CurrentWorld.TargetRays )
		{
			TargetLineRenderer.enabled = true;
			TargetLineRenderer.SetPosition( 0, transform.position );
			TargetLineRenderer.SetPosition( 1, Target.transform.position );
		}
		else
			TargetLineRenderer.enabled = false;
	}

	protected virtual bool HasProblematicEnergy()
	{
		return Energy < ProblematicEnergyPercentage;
	}

	protected virtual bool HasToReproduce()
	{
		return IsAdult && !IsPregnant && !HasProblematicEnergy() && ( SecondsAlive - LastReproductionTime ) >= ReproductionCooldown;
	}

	#endregion

	#region Actions

	protected virtual List<EntityAction> CreateActionsList()
	{
		return new List<EntityAction>() {
			new EntityAction( HasToGiveBirth, GiveBirthAction ),
			new EntityAction( HasToMove, MoveAction ),
			new EntityAction( HasToInteractWithTarget, InteractWithTargetAction ),
			new EntityAction( HasToSearch, SearchAndPathToTargetAction ),
			new EntityAction( HasToGrow, GrowAction ),
			new EntityAction( () => { return true; }, RandomMovementAction ),
		};
	}

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

	protected override void CellChange( WorldCell newCell )
	{
		PreviousCell = CurrentCell;
		Direction = newCell.TerrainPos - WorldPosition2D;
		OriginPosition = CurrentPositionToReal();
	}

	#region Give birth

	protected virtual bool HasToGiveBirth()
	{
		return IsPregnant && HasToMove() && PreviousCell.IsFree();
	}

	protected virtual float GiveBirthAction()
	{
		IsPregnant = false;
		LastReproductionTime = SecondsAlive;

		Entity child = Instantiate( this, transform.parent );
		PreviousCell.TrySetContent( child );
		child.Initialize( CurrentWorld, PreviousCell, ChildNormalMoveSeconds, ChildFastMoveDivisor );
		CurrentWorld.NewEntity( child );

		return GiveBirthCost;
	}

	#endregion

	#region Move

	protected virtual bool HasToMove()
	{
		return transform.position != CurrentPositionToReal();
	}

	protected virtual float MoveAction()
	{
		float cost = NullEnergyCost;

		float currentMovementSeconds = Target ? FastMoveSeconds : NormalMoveSeconds;
		MovementProgress = Mathf.Min( MovementProgress + Time.deltaTime / currentMovementSeconds, 1 );
		Vector3 destinyPosition = CurrentPositionToReal();
		if ( MovementProgress != 1 )
		{
			transform.position = new Vector3(
			Mathf.Lerp( OriginPosition.x, destinyPosition.x, MovementProgress ),
			Mathf.Lerp( OriginPosition.y, destinyPosition.y, MovementProgress ),
			Mathf.Lerp( OriginPosition.z, destinyPosition.z, MovementProgress ) );
		}
		else
		{
			transform.position = destinyPosition;
			MovementProgress = 0;
			cost = EndedMovement();
		}

		return cost;
	}

	protected virtual float EndedMovement()
	{
		float currentMovementSeconds = Target ? FastMoveSeconds : NormalMoveSeconds;
		float speed = 1 / currentMovementSeconds;
		return speed * Move1msCost;
	}

	#endregion

	#region Touching target

	protected virtual bool HasToInteractWithTarget()
	{
		bool hasToInteract = false;

		if ( Target )
		{
			if ( Target is Food )
				hasToInteract = Energy < MaxEnergyValue;
			else if ( Target is Entity )
				hasToInteract = HasToReproduce();
		}

		return hasToInteract && MathFunctions.IsTouchingObjective( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible );
	}

	protected virtual float InteractWithTargetAction()
	{
		float cost = NotActionCost;

		if ( Target is Food )
			cost = Eat();
		else if ( Target is Entity && IsFemale )
			cost = Reproduce();

		return cost;
	}

	protected virtual float Eat()
	{
		Food foodTarget = Target as Food;

		if ( Energy < MaxEnergyValue )
			IncrementEnergy( foodTarget.GetEnergy() );

		if ( Energy == MaxEnergyValue )
			Target = null;

		return EatingCost;
	}

	protected virtual float Reproduce()
	{
		Entity entityTarget = Target as Entity;

		if ( IsFemale )
		{
			entityTarget.IncrementEnergy( -entityTarget.Reproduce() );

			ChildNormalMoveSeconds = ( NormalMoveSeconds + entityTarget.NormalMoveSeconds ) / 2;
			ChildFastMoveDivisor = ( FastMoveDivisor + entityTarget.FastMoveDivisor ) / 2;

			IsPregnant = true;
		}
		else
			LastReproductionTime = SecondsAlive;

		Target = null;

		ParticleSystem particles = Instantiate( ReproductionParticles, this.transform.position, Quaternion.identity );
		Destroy( particles.gameObject, particles.main.duration );

		return ReproductionCost;
	}

	#endregion

	#region Search and move to target

	protected virtual bool HasToSearch()
	{
		bool hasToSearch = HasProblematicEnergy() || HasToReproduce();

		if ( !hasToSearch )
			Target = null;

		return hasToSearch;
	}

	protected virtual float SearchAndPathToTargetAction()
	{
		float cost = SearchCost;
		List<WorldObject> interestingObjs = new List<WorldObject>();
		WorldObject closestInterestingObj = null;
		float closestInterestingObjDistance = float.MaxValue;

		WorldObject currentWorldObject;
		float currentDistance;
		Vector2Int searchAreaTopLeftCorner = WorldPosition2D + Vector2Int.one * -1 * SearchRadius;
		Vector2Int position = Vector2Int.zero;
		for ( int x = 0; x <= SearchRadius * 2; x++ )
		{
			for ( int y = 0; y <= SearchRadius * 2; y++ )
			{
				position.x = searchAreaTopLeftCorner.x + x;
				position.y = searchAreaTopLeftCorner.y + y;
				if ( position != WorldPosition2D )
				{
					currentWorldObject = CurrentTerrain.GetCellContent( position );

					// If the object is interesting
					if ( IsInterestingObject( currentWorldObject ) )
					{
						interestingObjs.Add( currentWorldObject );

						// If is the closest
						currentDistance = position.Distance( WorldPosition2D );
						if ( currentDistance < closestInterestingObjDistance )
						{
							closestInterestingObjDistance = currentDistance;
							closestInterestingObj = currentWorldObject;
						}
					}
				}
			}
		}

		Target = TargetAtInterestingObjects( closestInterestingObj, interestingObjs );
		if ( Target )
			if ( cost < NullEnergyCost )
				cost = TryPathToTarget();
			else
				cost += TryPathToTarget();

		return cost;
	}

	protected virtual bool IsInterestingObject( WorldObject obj )
	{
		bool isInteresting = false;

		if ( HasProblematicEnergy() )
			isInteresting = obj is Food;
		else if ( HasToReproduce() )
		{
			Entity entityTarget = obj as Entity;
			if ( entityTarget != null )
				isInteresting = entityTarget.IsFemale != IsFemale && entityTarget.HasToReproduce();
		}

		return isInteresting;
	}

	protected virtual WorldObject TargetAtInterestingObjects( WorldObject closestInterestingObj, List<WorldObject> interestingObjs )
	{
		return closestInterestingObj;
	}

	protected virtual float TryPathToTarget()
	{
		float cost = NotActionCost;
		TargetAccesible = false;
		bool movementDone = false;

		List<Vector2Int> PathToTarget = MathFunctions.Pathfinding( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible, (int)SearchRadius );

		// If a path to target is possible
		if ( PathToTarget.Count > 0 )
		{
			TargetAccesible = MathFunctions.IsTouchingObjective( PathToTarget[PathToTarget.Count - 1], Target.WorldPosition2D, CurrentTerrain.IsPosAccesible );

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

	#region Grow

	protected virtual bool HasToGrow()
	{
		return !IsAdult && !HasProblematicEnergy() && SecondsAlive >= SecondsToGrow;
	}

	protected virtual float GrowAction()
	{
		transform.localScale = AdultScale;
		transform.position = CurrentPositionToReal();
		IsAdult = true;
		LastReproductionTime = SecondsAlive;

		return NotActionCost;
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
		if ( Energy > MinEnergyValue )
		{
			Vector3 energyBarLocalPos = EnergyBar.transform.localPosition;
			EnergyBar.transform.localPosition = new Vector3( Mathf.InverseLerp( MinEnergyValue, MaxEnergyValue, Energy ) - 1, energyBarLocalPos.y, energyBarLocalPos.z );
			EnergyBar.color = HasProblematicEnergy() ? ProblematicEnergyColor : GoodEnergyColor;
		}
		else
			IsAlive = false;
	}

	protected virtual void IncrementEnergy( float amount )
	{
		SetEnergy( Energy + amount );
	}

	public override void Destroy()
	{
		IsAlive = false;

		ParticleSystem particles = Instantiate( DeathParticles, this.transform.position, Quaternion.identity );
		Destroy( particles.gameObject, particles.main.duration );

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