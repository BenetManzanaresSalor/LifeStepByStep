using UnityEngine;

public class Food : WorldObject
{
	#region Attributes

	#region Settings

	[SerializeField] public float BaseEnergy = 300;
	[SerializeField] public float IterationsToEat = 10;
	[SerializeField] [Range( 0, 100 )] protected float RegenerationPercentagePerSecond = 5;
	[SerializeField] protected bool DestroyWhenEmpty = true;

	#endregion

	#region Functional

	public float Energy { get; protected set; }
	protected Vector3 InitialLocalScale;
	protected bool HasToRegenerate { get { return Energy < BaseEnergy; } }
	protected float RegenerationMultiplier { get { return RegenerationPercentagePerSecond / 100; } }

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		InitialLocalScale = Render.transform.localScale;
		Energy = BaseEnergy;
	}

	#endregion

	#region Be eaten

	public float GetEnergy()
	{
		float energyGet = BaseEnergy * Time.deltaTime / IterationsToEat;

		energyGet = -IncrementEnergy( -energyGet );

		return energyGet;
	}

	protected virtual float IncrementEnergy( float increment )
	{
		if ( increment < 0 )
			increment = -Mathf.Min( -increment, Energy );
		else
			increment = Mathf.Min( increment, BaseEnergy - Energy );

		Energy = Energy + increment;
		CurrentWorld.FoodEnergyChange( increment );

		if ( Energy > 0 )
			AdaptScale();
		else if ( DestroyWhenEmpty )
		{
			CurrentWorld.FoodDestroyed( this );
			Destroy();
		}

		return increment;
	}

	protected void AdaptScale()
	{
		float inverseLerp = Mathf.InverseLerp( 0, BaseEnergy, Energy );

		Render.transform.localScale = InitialLocalScale * inverseLerp;

		// Force repositioning to touch the ground
		CellChange( CurrentCell );
	}

	#endregion

	#region Regeneration

	protected virtual void Update()
	{
		if ( CurrentWorld.AutomaticSteping && HasToRegenerate )
			Regenerate();
	}

	protected virtual void Regenerate()
	{
		float increment = Time.deltaTime * BaseEnergy * RegenerationMultiplier;
		IncrementEnergy( increment );
	}

	#endregion

	#region External use

	public override string ToString()
	{
		return $"Food in world position {CurrentCell.TerrainPos}";
	}

	#endregion
}
