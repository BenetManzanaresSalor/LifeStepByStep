using UnityEngine;

public class Food : WorldObject
{
	#region Attributes

	#region Settings

	[Header( "Food settings" )]
	[SerializeField] protected float BaseEnergy = 100;
	[SerializeField] protected float actionsToEat = 10;
	[SerializeField] [Range( 0, 100 )] protected float RegenerationPercentagePerSecond = 10;

	#endregion

	#region Function attributes

	public float Energy
	{
		get
		{
			return energy;
		}
		protected set
		{
			energy = Mathf.Clamp( value, 0, BaseEnergy );

			if ( energy == 0 )
			{
				Destroy();
			}
			else
			{
				AdaptScale();
				HasToRegenerate = energy < BaseEnergy;
			}
		}
	}
	protected float energy;
	protected Vector3 InitialLocalScale;
	public float ActionsToEat { get { return actionsToEat; } }	
	protected bool HasToRegenerate;
	protected float RegenerationMultiplier { get { return RegenerationPercentagePerSecond / 100; } }

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		InitialLocalScale = transform.localScale;
		Energy = BaseEnergy;
	}

	#endregion

	#region Nutrients

	public float GetEnergy()
	{
		float obtainedNutrients = BaseEnergy / ActionsToEat;

		Energy -= obtainedNutrients;

		return obtainedNutrients;
	}

	protected void AdaptScale()
	{
		float inverseLerp = Mathf.InverseLerp( 0, BaseEnergy, Energy );

		transform.localScale = InitialLocalScale * inverseLerp;

		// Force repositioning to touch the base
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
		Energy += Time.deltaTime * BaseEnergy * RegenerationMultiplier;
	}

	#endregion

	#region Auxiliar

	public override string ToString()
	{
		return $"Food in world position {CurrentCell.TerrainPos}";
	}

	#endregion
}
