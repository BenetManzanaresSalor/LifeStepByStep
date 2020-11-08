using UnityEngine;


public class GameController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] private Vector3 PlayerOffset = Vector3.up;
	[SerializeField] private World CurrentWorld;
	[SerializeField] private FirstPersonController Player;
	[SerializeField] private GameUI UI;

	#endregion

	#region Functional

	private MainController MainController;
	private bool WasAutomaticStepingEnabled = false;

	#endregion

	#endregion

	#region Initialization

	public void Initialize( MainController mainController )
	{
		MainController = mainController;

		RestartWorld();
	}

	public void SetEnabled( bool enabled )
	{
		// Store automatic steping state
		if ( !enabled )
			WasAutomaticStepingEnabled = CurrentWorld.AutomaticSteping;

		CurrentWorld.AutomaticSteping = enabled && WasAutomaticStepingEnabled;

		UI.gameObject.SetActive( enabled );

		this.enabled = enabled;
	}

	#endregion

	#region Control by keyboard

	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.E ) )
			ToggleAutomaticSteping();
		else if ( Input.GetKeyDown( KeyCode.R ) )
			RestartWorld();
		else if ( Input.GetKeyDown( KeyCode.T ) )
			UI.ToggleStatisticsMode();
		else if ( Input.GetKeyDown( KeyCode.Escape ) )
			ReturnToMain();
	}

	#endregion

	#region Controls

	public void RestartWorld()
	{
		if ( CurrentWorld.AutomaticSteping )
			ToggleAutomaticSteping();

		SetPlayerPos( CurrentWorld.transform.position );

		CurrentWorld.Initialize( this, Player.transform );
		UI.Initialize( this, CurrentWorld );
		InitializePlayer();
	}

	public void ToggleAutomaticSteping()
	{
		CurrentWorld.ToggleAutomaticSteping();
		UI.AutomaticStepingToggled( CurrentWorld.AutomaticSteping );
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

	protected void InitializePlayer()
	{
		Player.Initialize( this );
		SetPlayerPos( CurrentWorld.GetClosestCellRealPos( Player.transform.position ) + PlayerOffset );
	}

	public void ReturnToMain() => MainController.ReturnToMain();

	#endregion

	#region External use

	public void GetTerrainLimits( out Vector3 minPos, out Vector3 maxPos )
	{
		CurrentWorld.Terrain.GetTerrainLimits( out minPos, out maxPos );
	}

	public void SelectCell( bool isCollision, RaycastHit hit )
	{
		WorldCell cell = null;

		if ( isCollision )
		{
			WorldObject worldObj = hit.transform.parent.GetComponent<WorldObject>();
			if ( worldObj != null )
				cell = worldObj.CurrentCell;
			else
				cell = CurrentWorld.GetClosestCell( hit.point );
		}

		UI.SetCellToDescribe( cell );
	}

	public void ApplySettings( bool useRandomSeed, int seed, float[] worldProb, bool[] entityBools, float[] entityValues )
	{
		CurrentWorld.SetSettings( useRandomSeed, seed, worldProb, entityBools, entityValues );
	}

	#endregion
}