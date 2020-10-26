using UnityEngine;

public class Food : WorldObject
{
	#region Attributes

	#region Settings

	[Header( "Food settings" )]
	[SerializeField] protected float BaseEnergy = 100;
	[SerializeField] public float SecondsToEat = 10;
	[SerializeField] [Range( 0, 100 )] protected float RegenerationPercentagePerSecond = 10;
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
		InitialLocalScale = transform.localScale;
		Energy = BaseEnergy;
	}

	#endregion

	#region Nutrients

	public float GetEnergy()
	{
		float obtainedNutrients = BaseEnergy * Time.deltaTime / SecondsToEat;
		obtainedNutrients = Mathf.Min( obtainedNutrients, Energy );

		Energy = Mathf.Clamp( Energy - obtainedNutrients, 0, BaseEnergy );

		if ( Energy == 0 && DestroyWhenEmpty )
			Destroy();
		else
			AdaptScale();

		return obtainedNutrients;
	}

	protected void AdaptScale()
	{
		float inverseLerp = Mathf.InverseLerp( 0, BaseEnergy, Energy );

		transform.localScale = InitialLocalScale * inverseLerp;

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
