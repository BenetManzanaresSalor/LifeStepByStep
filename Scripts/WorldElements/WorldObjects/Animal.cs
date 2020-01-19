using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Animal : Entity
{
	#region Attributes

	#region Settings

	[Header( "Animal settings" )]
	[SerializeField] [Min( NullEnergyCost )] protected float EatingCost;
	[SerializeField] [Min( 0 )] protected float NumMovementsPerBaseEnergy = 10;
	[SerializeField] [Range( MinEnergyValue, MaxEnergyValue )] protected float ProblematicEnergyPercentage = 25;
	[SerializeField] protected Image EnergyBar;
	[SerializeField] protected ParticleSystem DeathParticles;

	#endregion
	protected bool ProblematicEnergy { get { return Energy < ProblematicEnergyPercentage; } }
	protected Food FoodTarget { get { return Target as Food; } }

	#endregion

	#region Actions

	protected override float TouchingTargetAction()
	{
		if ( Energy < MaxEnergyValue )
		{
			IncrementEnergy( FoodTarget.GetEnergy() );
		}
		else
		{
			Target = null;
		}

		return EatingCost;
	}

	protected override bool IsInterestingObject( WorldObject obj )
	{
		return ProblematicEnergy && obj is Food;
	}

	protected override void OnInterestingObjects( WorldObject closestInterestingObject, List<WorldObject> interestingObjects )
	{
		Target = closestInterestingObject;
	}

	#endregion

	#region Energy and destroy

	protected override void SetEnergy( float value )
	{
		base.SetEnergy( value );
		if ( EnergyBar != null )
		{
			EnergyBar.fillAmount = Mathf.InverseLerp( MinEnergyValue, MaxEnergyValue, Energy );
		}
	}

	public override void DestroyWorldObject()
	{
		ParticleSystem particles = Instantiate( DeathParticles, this.transform.position, Quaternion.identity );

		Destroy( particles.gameObject, particles.main.duration );

		base.DestroyWorldObject();
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"Animal in world position {WorldPosition2D}";
	}

	#endregion
}
