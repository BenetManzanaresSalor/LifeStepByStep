using UnityEngine;
using UnityEngine.UI;

public class Animal : Entity
{
	#region Attributes

	#region Settings

	[Header( "Animal settings" )]
	[SerializeField] [Min( NullEnergyCost )] protected float EatingCost = 0;
	[SerializeField] protected Image EnergyBar;
	[SerializeField] [Range( MinEnergyValue, MaxEnergyValue )] protected float ProblematicEnergyPercentage = 25;

	[Header( "Animal state settings" )]
	[SerializeField] protected Color GoodEnergyColor = Color.green;
	[SerializeField] protected Color ProblematicEnergyColor = Color.red;
	[SerializeField] protected Image SearchingImage;
	[SerializeField] protected Image HasTargetImage;
	[SerializeField] protected LineRenderer TargetLineRenderer;
	[SerializeField] protected ParticleSystem DeathParticles;

	#endregion

	#region Function attributes

	protected bool HasProblematicEnergy { get { return Energy < ProblematicEnergyPercentage; } }

	#endregion

	#endregion

	#region Initialization

	protected override void Start()
	{
		base.Start();
		HasTargetImage.enabled = false;
	}

	#endregion

	#region Update step

	public override bool Step()
	{
		bool isAlive = base.Step();
		UpdateStateRenderer();

		return isAlive;
	}

	protected virtual void UpdateStateRenderer()
	{
		bool hasTarget = HasTarget;
		bool isSearching = !hasTarget && HasToSearch();

		SearchingImage.enabled = isSearching;

		HasTargetImage.enabled = hasTarget;

		if ( CurrentWorld.TargetRays )
		{
			TargetLineRenderer.enabled = hasTarget;
			if ( hasTarget )
			{
				TargetLineRenderer.SetPosition( 0, transform.position );
				TargetLineRenderer.SetPosition( 1, Target.transform.position );
			}
		}
		else
			TargetLineRenderer.enabled = false;
	}

	#endregion

	#region Actions

	protected override float TouchingTargetAction()
	{
		if ( Energy < MaxEnergyValue )
			IncrementEnergy( ( Target as Food ).GetEnergy() );

		if ( Energy == MaxEnergyValue )
			Target = null;

		return EatingCost;
	}

	protected override bool HasToSearch()
	{
		bool hasToSearch = HasProblematicEnergy;
		if ( !hasToSearch )
			Target = null;

		return hasToSearch;
	}

	protected override bool IsInterestingObject( WorldObject obj )
	{
		return HasProblematicEnergy && obj is Food;
	}

	#endregion

	#region Energy and destroy

	protected override void SetEnergy( float value )
	{
		base.SetEnergy( value );

		EnergyBar.fillAmount = Mathf.InverseLerp( MinEnergyValue, MaxEnergyValue, Energy );
		EnergyBar.color = HasProblematicEnergy ? ProblematicEnergyColor : GoodEnergyColor;
	}

	public override void Destroy()
	{
		ParticleSystem particles = Instantiate( DeathParticles, this.transform.position, Quaternion.identity );
		Destroy( particles.gameObject, particles.main.duration );

		base.Destroy();
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"Animal in world position {WorldPosition2D}";
	}

	#endregion
}
