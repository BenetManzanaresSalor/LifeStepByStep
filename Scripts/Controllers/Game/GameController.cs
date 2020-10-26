using UnityEngine;


public class GameController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected Vector3 PlayerOffset = Vector3.up;
	[SerializeField] public bool TargetRays = false;

	#endregion

	#region Function

	protected World World;
	protected FirstPersonController Player;
	protected GameUI UI;

	#endregion

	#endregion

	#region Initialization

	protected void Start()
	{
		World = FindObjectOfType<World>();
		Player = FindObjectOfType<FirstPersonController>();
		UI = FindObjectOfType<GameUI>();
		

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

	public void RestartWorld()
	{
		if ( World.AutomaticSteping )
			ToggleAutomaticSteping();

		SetPlayerPos( World.transform.position );
		World.Initialize( this, Player.transform );
		UI.Initialize( this );
		InitializePlayer();
	}

	public void ToggleAutomaticSteping()
	{
		World.ToggleAutomaticSteping();
		UI.SetAutomaticSteping( World.AutomaticSteping );
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
		SetPlayerPos( World.GetClosestCellRealPos( Player.transform.position ) + PlayerOffset );
	}

	#endregion

	#region External use

	public void GetTerrainLimits( out Vector3 minPos, out Vector3 maxPos )
	{
		World.Terrain.GetTerrainLimits( out minPos, out maxPos );
	}

	public void SelectCell( bool isCollision, RaycastHit hit )
	{
		WorldCell cell = null;

		if ( isCollision )
		{
			WorldObject worldObj = hit.transform.GetComponent<WorldObject>();
			if ( worldObj != null )
				cell = worldObj.CurrentCell;
			else
				cell = World.GetClosestCell( hit.point );
		}

		UI.SetCellToDescribe( cell );
	}

	public void ExitGame()
	{
		Debug.Log( "EXIT" );
		Application.Quit();
	}

	#endregion
}