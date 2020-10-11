using UnityEngine;


public class WorldController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected Vector3 PlayerOffset = Vector3.up;
	[SerializeField] public bool TargetRays = false;

	#endregion

	public GenericWorld CurrentWorld { get; protected set; }
	public LC_FirstPersonController Player { get; protected set; }
	public UIController UI { get; protected set; }

	#endregion

	#region Initialization

	protected void Start()
	{
		CurrentWorld = FindObjectOfType<GenericWorld>();
		Player = FindObjectOfType<LC_FirstPersonController>();
		UI = FindObjectOfType<UIController>();

		RestartWorld();
	}

	#endregion

	#region World control by interaction

	protected void Update()
	{
		if ( Input.GetKeyDown( KeyCode.E ) )
			ToggleAutomaticSteping();
		if ( Input.GetKeyDown( KeyCode.R ) )
			RestartWorld();
		//if ( Input.GetKeyDown( KeyCode.F1 ) ) UI.SetStatus( CurrentWorld.GetStatus() );
		//if ( Input.GetKeyDown( KeyCode.F2 ) ) CurrentWorld.ResetStatistics();
	}

	#endregion

	#region Controls

	public void ToggleAutomaticSteping()
	{
		CurrentWorld.ToggleAutomaticSteping();
		UI.SetAutomaticSteping( CurrentWorld.AutomaticSteping );
	}

	public void RestartWorld()
	{
		if ( CurrentWorld.AutomaticSteping )
			ToggleAutomaticSteping();

		SetPlayerPos( CurrentWorld.transform.position );
		CurrentWorld.Generate( this );
		IniPlayerPos();
	}

	protected void SetPlayerPos( Vector3 pos )
	{
		bool initiallyEnabled = Player.enabled;
		if ( initiallyEnabled )
			Player.enabled = false;

		Player.transform.position = pos;

		if ( initiallyEnabled )
			Player.enabled = true;
	}

	protected void IniPlayerPos()
	{
		SetPlayerPos( CurrentWorld.GetNearestTerrainRealPos( Player.transform.position ) + PlayerOffset );
	}

	public void ExitGame()
	{
		Debug.Log( "EXIT" );
		Application.Quit();
	}

	#endregion
}