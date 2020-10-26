using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "Control" )]
	[SerializeField] protected Image PlayPauseIcon;
	[SerializeField] protected Sprite PlaySprite;
	[SerializeField] protected Sprite PauseSprite;
	[SerializeField] protected WorldMap Map;

	[Header( "WorldObject info" )]
	[SerializeField] protected RectTransform WorldObjPanel;
	[SerializeField] protected TextMeshProUGUI WorldObjTypeText;
	[SerializeField] protected TextMeshProUGUI EnergyText;
	[SerializeField] protected RectTransform EntityPanel;
	[SerializeField] [Range( 0, 1 )] protected float DisabledAlpha = 0.3f;
	[SerializeField] protected Image IsFemaleImg;
	[SerializeField] protected Image IsMaleImg;
	[SerializeField] protected TextMeshProUGUI SecondsAliveText;
	[SerializeField] protected TextMeshProUGUI NormalSpeedText;
	[SerializeField] protected TextMeshProUGUI FastSpeedText;
	[SerializeField] protected Image IsWalkingImg;
	[SerializeField] protected Image IsRunningImg;
	[SerializeField] protected Image IsSearchingImg;
	[SerializeField] protected Image HasTargetImg;
	[SerializeField] protected Image EatImg;
	[SerializeField] protected Image ReproduceImg;
	[SerializeField] protected Image IsOldImg;

	[Header( "Analytic" )]
	[SerializeField] protected TextMeshProUGUI FpsText;

	#endregion

	#region Function

	protected GameController GameController;
	protected bool IsWorldObjSelected = false;
	protected bool IsEntitySelected = false;
	protected Food FoodSelected;
	protected Entity EntitySelected;

	#endregion

	#endregion

	public void Initialize( GameController gameController )
	{
		GameController = gameController;
		Map.Initialize();
	}

	protected void Update()
	{
		FpsText.text = $"{Mathf.RoundToInt( 1f / Time.smoothDeltaTime )} FPS";

		if ( IsWorldObjSelected )
			UpdateWorldObjInfo();
	}

	protected void UpdateWorldObjInfo()
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

	public void SetAutomaticSteping( bool enabled )
	{
		PlayPauseIcon.sprite = enabled ? PauseSprite : PlaySprite;
	}

	public void SetCellToDescribe( WorldCell cell )
	{
		IsWorldObjSelected = cell != null && cell.Content != null;
		if ( IsWorldObjSelected )
		{
			WorldObjPanel.gameObject.SetActive( true );
			WorldObjTypeText.text = cell.Content.GetType().Name;

			EntitySelected = cell.Content as Entity;
			IsEntitySelected = EntitySelected != null;
			EntityPanel.gameObject.SetActive( IsEntitySelected );

			if ( !IsEntitySelected )
				FoodSelected = cell.Content as Food;

			InitializeWorldObjInfo();
		}
		else
			WorldObjPanel.gameObject.SetActive( false );
	}

	protected void InitializeWorldObjInfo()
	{
		if ( EntitySelected )
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
}
