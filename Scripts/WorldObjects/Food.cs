using UnityEngine;

public class Food : WorldObject
{
	#region Attributes

	#region Settings

	[Header( "Food settings" )]
	[SerializeField] protected float BaseEnergy;
	[SerializeField] protected float actionsToEat;
	[SerializeField] [Range( 0, 100 )] protected float RegenerationPercentagePerSecond;

	#endregion

	#region Data accesors

	public float Energy
	{
		get
		{
			return energy;
		}
		protected set
		{
			energy = MathFunctions.Clamp( value, 0, BaseEnergy );

			if ( energy == 0 )
			{
				DestroyWorldObject();
			}
			else
			{
				AdaptScale();

				HasToRegenerate = energy < BaseEnergy;
			}
		}
	}
	public float ActionsToEat { get { return actionsToEat; } }

	#endregion

	protected float energy;

	protected Vector3 InitialLocalScale;

	protected bool HasToRegenerate;
	protected float RegenerationMultiplier { get { return RegenerationPercentagePerSecond / 100; } }

	#endregion

	#region Initialization

	protected override void Start()
	{
		base.Start();

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
		{
			Regenerate();
		}
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
