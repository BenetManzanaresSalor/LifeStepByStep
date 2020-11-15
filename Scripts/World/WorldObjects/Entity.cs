using System;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// <para>Main actuator of the world, able of move, grow, search, do pathfinding, eat, reproduce and die by age.</para>
/// <para>Child of WorldObject.</para>
/// <para>Controlled by World.</para>
/// </summary>
public class Entity : WorldObject
{
	#region EntityAction sctruct

	/// <summary>
	/// <para>Struct created to define an action of the Entity.</para>
	/// <para>An action is represented by a Condition (function which returns bool) and Method (function which returns float, the cost)</para>
	/// </summary>
	protected struct EntityAction
	{
		public Func<bool> Condition;
		public Func<float> Method;

		public EntityAction( Func<bool> condition, Func<float> method )
		{
			Condition = condition;
			Method = method;
		}
	}

	#endregion

	#region Constants

	// Energy
	protected const float MaxEnergyValue = 100;
	protected const float MinEnergyValue = 0;

	// Actions costs
	protected const float NotActionCost = -1;   // The entity can do more actions after an action with this cost or less.
	protected const float NullEnergyCost = 0;   // The entity cannot do more actions after an action with this cost or grater.	

	#endregion

	#region Attributes

	#region Settings

	[Header( "Movement" )]
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float Move1msCost = 1.5f;
	[SerializeField] protected Vector2 MinAndMaxMoveSeconds = new Vector2( 1f, 1.25f );
	[SerializeField] protected Vector2 MinAndMaxFastMoveDivisor = new Vector2( 1f, 3f );
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float RandomMoveCost = NullEnergyCost;
	[SerializeField] [Range( 0, 100 )] protected float ConserveDirectionProbability = 80;

	[Header( "Searching and pathfinding" )]
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float SearchCost = NotActionCost;
	[SerializeField] [Range( NotActionCost, MaxEnergyValue )] protected float PathToTargetCost = NullEnergyCost;
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float EatingCost = 0;

	[Header( "Growing and reproduction" )]
	[SerializeField] protected Vector2 MinAndMaxSecondsToGrow = new Vector2( 10f, 20f );
	[SerializeField] protected Vector3 ChildScale = Vector3.one * 0.6f;
	[SerializeField] protected Vector3 AdultScale = Vector3.one;
	[SerializeField] protected Vector2 MinAndMaxReproductionCooldown = new Vector2( 30f, 60f );
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float ReproductionCost = 15;
	[SerializeField] [Range( NullEnergyCost, MaxEnergyValue )] protected float GiveBirthCost = 25;

	[Header( "Death" )]
	[SerializeField] protected Vector2 MinAndMaxSecondsToOld = new Vector2( 120f, 180f );

	[Header( "Information render" )]
	[SerializeField] protected SpriteRenderer FemenineSprite;
	[SerializeField] protected SpriteRenderer MasculineSprite;
	[SerializeField] protected GameObject EnergyBarContainer;
	[SerializeField] protected Transform EnergyBar;
	[SerializeField] protected SpriteRenderer EnergyBarSprite;
	[SerializeField] protected Color GoodEnergyColor = Color.green;
	[SerializeField] protected Color ProblematicEnergyColor = Color.red;
	[SerializeField] protected GameObject StateIconsPanel;
	[SerializeField] protected SpriteRenderer IsSearchingSprite;
	[SerializeField] protected SpriteRenderer HasTargetSprite;
	[SerializeField] protected SpriteRenderer EatSprite;
	[SerializeField] protected SpriteRenderer ReproduceSprite;
	[SerializeField] protected SpriteRenderer IsOldSprite;
	[SerializeField] protected LineRenderer TargetRay;
	[SerializeField] protected ParticleSystem ReproductionParticles;
	[SerializeField] protected ParticleSystem DeathParticles;

	#endregion

	#region Functional

	public bool IsAlive { get; protected set; }
	public float SecondsAlive { get; protected set; }
	public float Energy { get; protected set; }
	protected System.Random RandomGenerator;
	protected List<EntityAction> ActionsList;

	protected float MovementProgress;
	public float NormalMoveSeconds { get; protected set; }
	public float FastMoveDivisor { get; protected set; }
	public float FastMoveSeconds { get; protected set; }
	protected Vector3 OriginPosition;
	public Vector2Int Direction
	{
		get { return direction; }
		protected set
		{
			direction = value;

			transform.rotation = Quaternion.identity;

			if ( direction != Vector2Int.zero )
				transform.rotation *= Quaternion.LookRotation( new Vector3( direction.x, 0, direction.y ) );
		}
	}
	protected Vector2Int direction;

	protected bool TargetAccesible = false;
	public WorldObject Target { get; protected set; }
	protected WorldObject LastTarget;
	protected List<Vector2Int> PathToTarget;

	protected float SecondsToGrow;
	public bool IsAdult { get; protected set; }
	public bool IsFemale { get; protected set; }
	protected float ReproductionCooldown;
	protected float LastReproductionTime;
	protected bool IsPregnant = false;
	protected WorldCell PreviousCell;
	protected float ChildNormalMoveSeconds;
	protected float ChildFastMoveDivisor;

	protected float SecondsToOld;
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
		NormalMoveSeconds = (float)MathFunctions.RandomDouble( RandomGenerator, MinAndMaxMoveSeconds );
		FastMoveDivisor = (float)MathFunctions.RandomDouble( RandomGenerator, MinAndMaxFastMoveDivisor );
		FastMoveSeconds = NormalMoveSeconds / FastMoveDivisor;
		MovementProgress = 0;

		IsFemale = RandomGenerator.NextDouble() >= 0.5f;
		FemenineSprite.enabled = IsFemale;
		MasculineSprite.enabled = !IsFemale;

		Render.transform.localScale = ChildScale.Div( transform.parent.lossyScale );
		transform.position = CurrentPositionToReal();

		SecondsToGrow = (float)MathFunctions.RandomDouble( RandomGenerator, MinAndMaxSecondsToGrow );
		ReproductionCooldown = (float)MathFunctions.RandomDouble( RandomGenerator, MinAndMaxReproductionCooldown );

		SecondsToOld = (float)MathFunctions.RandomDouble( RandomGenerator, MinAndMaxSecondsToOld );

		ActionsList = CreateActionsList();

		UpdateStateRenderer();
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
			IncrementEnergy( -DoAction() );
			UpdateStateRenderer();
		}

		return IsAlive;
	}

	protected virtual void UpdateStateRenderer()
	{
		GetState( out bool hasTarget, out bool isSearching, out bool eat, out bool reproduce, out bool isOld );

		// Energy bar
		EnergyBarContainer.gameObject.SetActive( CurrentWorld.ShowEnergyBar );

		// State sprites
		StateIconsPanel.SetActive( CurrentWorld.ShowStateIcons );
		if ( CurrentWorld.ShowStateIcons )
		{
			IsSearchingSprite.enabled = isSearching;
			HasTargetSprite.enabled = hasTarget;
			EatSprite.enabled = eat;
			ReproduceSprite.enabled = reproduce;
			IsOldSprite.enabled = isOld;
		}

		// Target ray
		if ( hasTarget && CurrentWorld.ShowTargetRays )
		{
			TargetRay.enabled = true;
			TargetRay.SetPosition( 0, transform.position );
			TargetRay.SetPosition( 1, Target.transform.position );
		}
		else
			TargetRay.enabled = false;
	}

	public virtual void GetState( out bool hasTarget, out bool isSearching, out bool eat, out bool reproduce, out bool isOld )
	{
		hasTarget = HasTarget();
		isSearching = !hasTarget && HasToSearch();

		if ( hasTarget )
		{
			eat = Target is Food;
			reproduce = Target is Entity;
		}
		else if ( isSearching )
		{
			eat = HasToEat();
			reproduce = !eat && HasToReproduce();
		}
		else
		{
			eat = false;
			reproduce = false;
		}

		isOld = IsOld();
	}

	#endregion

	#region Actions

	protected virtual List<EntityAction> CreateActionsList()
	{
		return new List<EntityAction>() {
			new EntityAction( IsOld, MaybeDeathByAge ),
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
		bool canDoMoreActions = false;
		EntityAction action;

		for ( int i = 0; i < ActionsList.Count && !canDoMoreActions; i++ )
		{
			action = ActionsList[i];
			if ( action.Condition() )
			{
				cost = action.Method();
				canDoMoreActions = cost >= NullEnergyCost;
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

	#region Death by age

	public virtual bool IsOld()
	{
		return SecondsAlive > SecondsToOld;
	}

	protected virtual float MaybeDeathByAge()
	{
		// If death by age is enabled and is a different second
		if ( CurrentWorld.DeathByAge && LastSecondOld != (int)SecondsAlive )
		{
			float deathProbabilty = 1 - ( SecondsToOld / SecondsAlive );
			double randomValue = RandomGenerator.NextDouble();
			if ( deathProbabilty > randomValue )
				Die( true );

			LastSecondOld = (int)SecondsAlive;
		}

		return IsAlive ? NotActionCost : NullEnergyCost;
	}

	#endregion

	#region Give birth

	public virtual bool HasToGiveBirth()
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

	public virtual bool HasToMove()
	{
		return transform.position != CurrentPositionToReal();
	}

	protected virtual float MoveAction()
	{
		float cost = NullEnergyCost;

		float currentMovementSeconds = HasToRun() ? FastMoveSeconds : NormalMoveSeconds;
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

	public virtual bool HasToRun()
	{
		return Target != null;
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

		if ( HasTarget() )
		{
			hasToInteract = CanEat();
			if ( !hasToInteract )
				hasToInteract = CanReproduce();
		}

		return hasToInteract && MathFunctions.IsTouchingTarget( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible );
	}

	public virtual bool HasTarget()
	{
		return Target != null;
	}

	public virtual bool CanEat()
	{
		return HasToEat() || Target is Food && Energy < MaxEnergyValue;
	}

	public virtual bool HasToEat()
	{
		return HasProblematicEnergy();
	}

	public virtual bool HasProblematicEnergy()
	{
		return Energy < CurrentWorld.ProblematicEnergyPercentage;
	}

	public virtual bool CanReproduce()
	{
		return HasToReproduce() && Target is Entity;
	}

	public virtual bool HasToReproduce()
	{
		return IsAdult && !IsPregnant && !HasToEat() && ( SecondsAlive - LastReproductionTime ) >= ReproductionCooldown;
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

	public virtual bool HasToSearch()
	{
		bool hasToSearch = HasToEat() || HasToReproduce();

		if ( !hasToSearch )
			Target = null;

		return hasToSearch;
	}

	protected virtual float SearchAndPathToTargetAction()
	{
		float cost = SearchCost;
		Target = null;

		WorldObject currentWorldObj;
		Vector2Int topLeftCorner;
		Vector2Int position = Vector2Int.zero;
		int radius, x, y, yIncrement;

		// Incremental radius search
		for ( radius = 1; radius < CurrentWorld.SearchRadius && Target == null; radius++ )
		{
			topLeftCorner = WorldPosition2D - Vector2Int.one * radius;

			for ( x = 0; x <= radius * 2 && Target == null; x++ )
			{
				yIncrement = ( x == 0 || x == radius * 2 ) ? 1 : radius * 2;
				for ( y = 0; y <= radius * 2 && Target == null; y += yIncrement )
				{
					position.x = topLeftCorner.x + x;
					position.y = topLeftCorner.y + y;

					if ( position != WorldPosition2D )
					{
						currentWorldObj = CurrentTerrain.GetCellContent( position );
						if ( IsInterestingObj( currentWorldObj ) )
							Target = currentWorldObj;
					}
				}
			}
		}

		if ( Target != null )
			if ( cost < NullEnergyCost )
				cost = TryPathToTarget();
			else
				cost += TryPathToTarget();

		return cost;
	}

	protected virtual bool IsInterestingObj( WorldObject obj )
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

	protected virtual float TryPathToTarget()
	{
		float cost = NotActionCost;
		TargetAccesible = false;
		bool movementDone = false;

		if ( Target == LastTarget )
			PathToTarget = MathFunctions.PathfindingWithReusing( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible, CurrentWorld.SearchRadius * 2, PathToTarget );
		else
			PathToTarget = MathFunctions.Pathfinding( WorldPosition2D, Target.WorldPosition2D, CurrentTerrain.IsPosAccesible, CurrentWorld.SearchRadius );

		LastTarget = Target;

		// If a path to target is possible
		if ( PathToTarget.Count > 0 )
		{
			TargetAccesible = MathFunctions.IsTouchingTarget( PathToTarget[PathToTarget.Count - 1], Target.WorldPosition2D, CurrentTerrain.IsPosAccesible );

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

	public virtual bool HasToGrow()
	{
		return !IsAdult && !HasToEat() && SecondsAlive >= SecondsToGrow;
	}

	protected virtual float GrowAction()
	{
		Render.transform.localScale = AdultScale;
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

	#region Energy and death

	protected virtual void IncrementEnergy( float increment )
	{
		if ( increment != 0 )
			SetEnergy( Energy + increment );
	}

	protected virtual void SetEnergy( float value )
	{
		Energy = Mathf.Clamp( value, MinEnergyValue, MaxEnergyValue );
		if ( Energy > MinEnergyValue )
		{
			Vector3 barLocalScale = EnergyBar.transform.localScale;
			EnergyBar.transform.localScale = new Vector3( Mathf.InverseLerp( MinEnergyValue, MaxEnergyValue, Energy ), barLocalScale.y, barLocalScale.z );
			EnergyBarSprite.color = HasProblematicEnergy() ? ProblematicEnergyColor : GoodEnergyColor;
		}
		else
		{
			Die( false );
		}
	}

	public virtual void Die( bool byAge )
	{
		IsAlive = false;

		CurrentWorld.EntityDie( this, byAge );

		ParticleSystem particles = Instantiate( DeathParticles, this.transform.position, Quaternion.identity );
		Destroy( particles.gameObject, particles.main.duration );

		Destroy();
	}

	#endregion

	#region External use

	public override string ToString()
	{
		return $"Entity in world position {WorldPosition2D}";
	}

	#endregion
}