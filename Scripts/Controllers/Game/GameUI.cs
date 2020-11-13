using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>Controls the game UI, including bottom, world object and statistics panels.</para>
/// <para>In bottom panel, it also initializes WorldMap.</para>
/// <para>Controlled by GameController.</para>
/// </summary>
public class GameUI : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "Control" )]
	[SerializeField] private Image PlayPauseIcon;
	[SerializeField] private Sprite PlaySprite;
	[SerializeField] private Sprite PauseSprite;
	[SerializeField] private WorldMap Map;

	[Header( "WorldObject info" )]
	[SerializeField] private RectTransform WorldObjPanel;
	[SerializeField] private TextMeshProUGUI WorldObjTypeText;
	[SerializeField] private TextMeshProUGUI EnergyText;
	[SerializeField] private RectTransform EntityPanel;
	[SerializeField] [Range( 0, 1 )] private float DisabledAlpha = 0.3f;
	[SerializeField] private Image IsFemaleImg;
	[SerializeField] private Image IsMaleImg;
	[SerializeField] private TextMeshProUGUI SecondsAliveText;
	[SerializeField] private TextMeshProUGUI NormalSpeedText;
	[SerializeField] private TextMeshProUGUI FastSpeedText;
	[SerializeField] private Image IsWalkingImg;
	[SerializeField] private Image IsRunningImg;
	[SerializeField] private Image IsSearchingImg;
	[SerializeField] private Image HasTargetImg;
	[SerializeField] private Image EatImg;
	[SerializeField] private Image ReproduceImg;
	[SerializeField] private Image IsOldImg;

	[Header( "Statistics" )]
	[SerializeField] private RectTransform StatisticsPanel;
	[SerializeField] private TextMeshProUGUI FpsText;
	[SerializeField] private TextMeshProUGUI NumEntitesText;
	[SerializeField] private TextMeshProUGUI NumBornEntitesText;
	[SerializeField] private TextMeshProUGUI NumDeadEntitesText;
	[SerializeField] private TextMeshProUGUI NumDeathsByAgeText;
	[SerializeField] private TextMeshProUGUI NumDeathsByEnergyText;
	[SerializeField] private TextMeshProUGUI NumFoodsText;
	[SerializeField] private TextMeshProUGUI FoodsEnergyText;
	[SerializeField] private TextMeshProUGUI EnergyPerEntityText;

	#endregion

	#region Functional

	private GameController GameController;
	private World World;
	private WorldObject WorldObjSelected;
	private Food FoodSelected;
	private Entity EntitySelected;
	private bool IsEntitySelected = false;
	private bool InAnalyticMode;

	#endregion

	#endregion

	#region Initialization

	public void Initialize( GameController gameController, World world )
	{
		GameController = gameController;
		World = world;

		Map.Initialize();
		WorldObjPanel.gameObject.SetActive( IsWorldObjSelected() );
		StatisticsPanel.gameObject.SetActive( InAnalyticMode );
	}

	#endregion

	#region Update

	private void Update()
	{
		UpdateWorldObjInfo();
		UpdateAnalyticUI();
	}

	private void UpdateWorldObjInfo()
	{
		if ( IsWorldObjSelected() )
		{
			EnergyText.text = ( IsEntitySelected ? EntitySelected.Energy : FoodSelected.Energy ).ToString( "f0" );

			if ( IsEntitySelected )
			{
				SecondsAliveText.text = EntitySelected.SecondsAlive.ToString( "f0" );

				EntitySelected.GetState( out bool hasTarget, out bool isSearching, out bool eat, out bool reproduce, out bool isOld );

				Color color;

				color = IsWalkingImg.color;
				color.a = !hasTarget ? 1f : DisabledAlpha;
				IsWalkingImg.color = color;

				color = IsRunningImg.color;
				color.a = hasTarget ? 1f : DisabledAlpha;
				IsRunningImg.color = color;

				color = IsSearchingImg.color;
				color.a = isSearching ? 1f : DisabledAlpha;
				IsSearchingImg.color = color;

				color = HasTargetImg.color;
				color.a = hasTarget ? 1f : DisabledAlpha;
				HasTargetImg.color = color;

				color = EatImg.color;
				color.a = eat ? 1f : DisabledAlpha;
				EatImg.color = color;

				color = ReproduceImg.color;
				color.a = reproduce ? 1f : DisabledAlpha;
				ReproduceImg.color = color;

				color = IsOldImg.color;
				color.a = isOld ? 1f : DisabledAlpha;
				IsOldImg.color = color;
			}
		}
		else if ( WorldObjPanel.gameObject.activeInHierarchy )
		{
			WorldObjPanel.gameObject.SetActive( false );
		}
	}

	private bool IsWorldObjSelected()
	{
		bool isSelected = false;

		if ( IsEntitySelected )
			isSelected = EntitySelected != null && EntitySelected.IsAlive;
		else
			isSelected = FoodSelected != null;

		return isSelected;
	}

	#endregion

	#region Normal mode

	public void SetCellToDescribe( WorldCell cell )
	{
		// Disable select of last object if that exists
		if ( WorldObjSelected != null )
			WorldObjSelected.SetSelected( false );

		if ( cell != null && cell.Content != null )
		{
			WorldObjSelected = cell.Content;
			WorldObjSelected.SetSelected( true );

			EntitySelected = WorldObjSelected as Entity;
			FoodSelected = WorldObjSelected as Food;
			IsEntitySelected = EntitySelected != null;

			if ( IsWorldObjSelected() )
				InitializeWorldObjInfo();
		}
		else
		{
			WorldObjPanel.gameObject.SetActive( false );
			FoodSelected = null;
			EntitySelected = null;
		}
	}

	private void InitializeWorldObjInfo()
	{
		WorldObjPanel.gameObject.SetActive( true );
		EntityPanel.gameObject.SetActive( IsEntitySelected );

		WorldObjTypeText.text = WorldObjSelected.GetType().Name;

		if ( IsEntitySelected )
		{
			IsFemaleImg.enabled = EntitySelected.IsFemale;
			IsMaleImg.enabled = !EntitySelected.IsFemale;

			float normalSpeed = 1f / EntitySelected.NormalMoveSeconds;
			NormalSpeedText.text = normalSpeed.ToString( "f3" );

			float fastSpeed = 1f / EntitySelected.FastMoveSeconds;
			FastSpeedText.text = fastSpeed.ToString( "f3" );
		}

		UpdateWorldObjInfo();
	}

	public void ToggleAutomaticStepping() => GameController.ToggleAutomaticSteping();

	public void AutomaticStepingToggled( bool enabled ) => PlayPauseIcon.sprite = enabled ? PauseSprite : PlaySprite;

	public void ResetWorld() => GameController.RestartWorld();

	public void ReturnToMain() => GameController.ReturnToMain();

	#endregion

	#region Statistics mode

	public void ToggleStatisticsMode()
	{
		InAnalyticMode = !InAnalyticMode;
		StatisticsPanel.gameObject.SetActive( InAnalyticMode );

		if ( InAnalyticMode )
			UpdateAnalyticUI();
	}

	private void UpdateAnalyticUI()
	{
		if ( InAnalyticMode )
		{
			FpsText.text = $"{Mathf.RoundToInt( 1f / Time.smoothDeltaTime )} FPS";

			int numEntities = World.NumEntites;
			NumEntitesText.text = numEntities.ToString();
			NumBornEntitesText.text = World.NumBornEntities.ToString();
			NumDeadEntitesText.text = World.NumDeadEntities.ToString();
			NumDeathsByAgeText.text = World.NumDeathsByAge.ToString();
			NumDeathsByEnergyText.text = World.NumDeathsByEnergy.ToString();

			NumFoodsText.text = World.NumFoods.ToString();
			float totalFoodsEnergy = World.TotalFoodsEnergy;
			FoodsEnergyText.text = totalFoodsEnergy.ToString( "f0" );
			float energyPerEntity = totalFoodsEnergy / numEntities;
			EnergyPerEntityText.text = energyPerEntity.ToString( "f0" );
		}
	}

	#endregion
}
