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
	[SerializeField] protected Color GoodEnergyColor = Color.green;
	[SerializeField] protected Color ProblematicEnergyColor = Color.red;
	[SerializeField] protected Image HasTargetImage;
	[SerializeField] protected ParticleSystem DeathParticles;

	#endregion

	#region Function attributes

	protected bool HasProblematicEnergy { get { return Energy < ProblematicEnergyPercentage; } }
	protected Food FoodTarget { get { return Target as Food; } }

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
		HasTargetImage.enabled = Target != null;
		return base.Step();
	}

	#endregion

	#region Actions

	protected override float TouchingTargetAction()
	{
		if ( Energy < MaxEnergyValue )
			IncrementEnergy( FoodTarget.GetEnergy() );

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
