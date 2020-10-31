using UnityEngine;

public class Food : WorldObject
{
	#region Attributes

	#region Settings

	[Header( "Food settings" )]
	[SerializeField] public float BaseEnergy = 300;
	[SerializeField] public float IterationsToEat = 10;
	[SerializeField] [Range( 0, 100 )] protected float RegenerationPercentagePerSecond = 5;
	[SerializeField] protected bool DestroyWhenEmpty = true;

	#endregion

	#region Function attributes

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

	#region Nutrients

	public float GetEnergy()
	{
		float energyDecrement = 0;

		if ( Energy > 0 )
		{
			energyDecrement = BaseEnergy * Time.deltaTime / IterationsToEat;
			energyDecrement = Mathf.Min( energyDecrement, Energy );
			Energy = Energy - energyDecrement;
			CurrentWorld.FoodEnergyChange( -energyDecrement );

			if ( Energy == 0 && DestroyWhenEmpty )
				CurrentWorld.FoodDestroyed( this );
			else
				AdaptScale();
		}

		return energyDecrement;
	}

	protected void AdaptScale()
	{
		float inverseLerp = Mathf.InverseLerp( 0, BaseEnergy, Energy );

		Render.transform.localScale = InitialLocalScale * inverseLerp;

		// Force repositioning to touch floor
		CellChange( CurrentCell );
	}

	#endregion

	#region Regeneration

	protected virtual void Update()
	{
		if ( CurrentWorld.AutomaticSteping && HasToRegenerate )
		{
			Regenerate();
			AdaptScale();
		}
	}

	protected virtual void Regenerate()
	{
		float increment = Time.deltaTime * BaseEnergy * RegenerationMultiplier;
		Energy += increment;
		CurrentWorld.FoodEnergyChange( increment );
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"Food in world position {CurrentCell.TerrainPos}";
	}

	#endregion
}
