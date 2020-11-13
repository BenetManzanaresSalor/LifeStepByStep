using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>Controls the main menu UI, including instructions and settings panels.</para>
/// <para>Controlled by MainController.</para>
/// </summary>
public class MainUI : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "Global" )]
	[SerializeField] private RectTransform MainPanel;
	[SerializeField] private TextMeshProUGUI[] StartButtonTexts;
	[SerializeField] private RectTransform IntructionsPanel;

	[Header( "Settings inputs" )]
	[SerializeField] private RectTransform SettingsPanel;
	[SerializeField] private Toggle UseRandomSeedToggle;
	[SerializeField] private TMP_InputField SeedInputField;
	[SerializeField] private Slider[] WorldSliders;
	[SerializeField] private TextMeshProUGUI[] WorldTexts;
	[SerializeField] private Toggle DeathByAgeToggle;
	[SerializeField] private Toggle ShowStateIconsToggle;
	[SerializeField] private Toggle ShowEnergyBarToggle;
	[SerializeField] private Toggle ShowTargetRaysToggle;
	[SerializeField] private Slider[] EntitySliders;
	[SerializeField] private TextMeshProUGUI[] EntityTexts;

	#endregion

	#region Functional

	private MainController MainController;
	private bool IsGameStarted = false;

	#endregion

	#endregion

	#region Initialization

	public void Initialize( MainController mainController )
	{
		MainController = mainController;

		ResetToDefaults();
		ReturnToMain();
	}

	#endregion

	#region Control

	public void Play()
	{
		if ( !IsGameStarted )
		{
			foreach ( TextMeshProUGUI textMesh in StartButtonTexts )
				textMesh.text = "Continue (Esc)";

			IsGameStarted = true;
		}

		MainController.Play();
	}

	public void Instructions()
	{
		MainPanel.gameObject.SetActive( false );
		IntructionsPanel.gameObject.SetActive( true );
	}

	public void Settings()
	{
		MainPanel.gameObject.SetActive( false );
		SettingsPanel.gameObject.SetActive( true );
	}

	public void GetSettings( out bool useRandomSeed, out int seed, out float[] worldProb, out bool[] entityBools, out float[] entityValues )
	{
		useRandomSeed = UseRandomSeedToggle.isOn;
		if ( SeedInputField.text != "" )
			seed = int.Parse( SeedInputField.text );
		else
			seed = 0;

		worldProb = new float[WorldSliders.Length];
		for ( int i = 0; i < WorldSliders.Length; i++ )
			worldProb[i] = WorldSliders[i].value;

		entityBools = new bool[4];
		entityBools[0] = DeathByAgeToggle.isOn;
		entityBools[1] = ShowStateIconsToggle.isOn;
		entityBools[2] = ShowEnergyBarToggle.isOn;
		entityBools[3] = ShowTargetRaysToggle.isOn;

		entityValues = new float[EntitySliders.Length];
		for ( int i = 0; i < EntitySliders.Length; i++ )
			entityValues[i] = EntitySliders[i].value;
	}

	public void ResetToDefaults()
	{
		UseRandomSeedToggle.isOn = MainController.DefaultUseRandomSeed;

		for ( int i = 0; i < WorldSliders.Length; i++ )
			WorldSliders[i].value = MainController.DefaultWorldProbs[i];

		DeathByAgeToggle.isOn = MainController.DefaultDeathByAge;
		ShowStateIconsToggle.isOn = MainController.DefaultShowStateIcons;
		ShowEnergyBarToggle.isOn = MainController.DefaultShowEnergyBar;
		ShowTargetRaysToggle.isOn = MainController.DefaultShowTargetRays;

		for ( int i = 0; i < EntitySliders.Length; i++ )
			EntitySliders[i].value = MainController.DefaultEntityValues[i];
	}

	public void ReturnToMain()
	{
		IntructionsPanel.gameObject.SetActive( false );
		SettingsPanel.gameObject.SetActive( false );
		MainPanel.gameObject.SetActive( true );
	}

	public void Exit() => MainController.Exit();

	#endregion

	#region Auxiliar

	public void WorldSliderUpdate( int idx )
	{
		WorldTexts[idx].text = WorldSliders[idx].value.ToString( "f1" ) + "%";
	}

	public void EntitySliderUpdate( int idx )
	{
		EntityTexts[idx].text = EntitySliders[idx].value.ToString( "f1" );
	}

	#endregion
}
