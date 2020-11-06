using UnityEngine;

public class MainController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] private FirstPersonController Player;
	[SerializeField] private MainUI UI;
	[SerializeField] private GameController GameController;

	[Header( "Default game settings" )]
	[SerializeField] public bool DefaultUseRandomSeed;
	[SerializeField] public float DefaultSeed;
	[SerializeField] public float[] DefaultWorldProbs;
	[SerializeField] public bool DefaultDeathByAge;
	[SerializeField] public bool DefaultShowStateIcons;
	[SerializeField] public bool DefaultShowEnergyBar;
	[SerializeField] public bool DefaultShowTargetRays;
	[SerializeField] public float[] DefaultEntityValues;

	#endregion

	#region Function

	private bool IsGameStarted = false;
	private bool IsPlaying = false;

	#endregion

	#endregion

	#region Initialization

	public void Start()
	{
		UI.gameObject.SetActive( true );
		UI.Initialize( this );

		GameController.gameObject.SetActive( false );
	}

	#endregion

	private void Update()
	{
		if ( IsGameStarted && Input.GetKeyDown( KeyCode.Escape ) )
		{
			if ( IsPlaying )
				ReturnToMain();
			else
				Play();
		}
	}

	#region External use

	public void Play()
	{
		UI.gameObject.SetActive( false );

		ApplySettings();
		if ( !IsGameStarted )
		{
			GameController.gameObject.SetActive( true );
			GameController.Initialize( this );
			IsGameStarted = true;
		}
		GameController.SetEnabled( true );

		Player.enabled = true;

		IsPlaying = true;
	}

	public void ReturnToMain()
	{
		GameController.SetEnabled( false );
		UI.gameObject.SetActive( true );
		Player.enabled = false;

		IsPlaying = false;
	}

	public void ApplySettings()
	{
		UI.GetSettings( out bool useRandomSeed, out int seed, out float[] worldProb, out bool[] entityBools, out float[] entityValues );
		GameController.ApplySettings( useRandomSeed, seed, worldProb, entityBools, entityValues );
	}

	public void Exit() => Application.Quit();

	#endregion
}
