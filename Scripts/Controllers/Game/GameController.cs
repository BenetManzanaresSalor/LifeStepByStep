using UnityEngine;


public class GameController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected Vector3 PlayerOffset = Vector3.up;
	[SerializeField] public bool TargetRays = false;
	[SerializeField] protected World World;
	[SerializeField] protected FirstPersonController Player;
	[SerializeField] protected GameUI UI;

	#endregion

	#region Function

	protected MainController MainController;

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
		UI.gameObject.SetActive( enabled );
	}

	#endregion

	#region World control by interaction

	protected void Update()
	{
		if ( Input.GetKeyDown( KeyCode.E ) )
			ToggleAutomaticSteping();
		if ( Input.GetKeyDown( KeyCode.R ) )
			RestartWorld();
		if ( Input.GetKeyDown( KeyCode.Escape ) )
			ReturnToMain();
		//if ( Input.GetKeyDown( KeyCode.F1 ) ) UI.SetStatus( CurrentWorld.GetStatus() ); // TODO
	}

	#endregion

	#region Controls

	public void RestartWorld()
	{
		if ( World.AutomaticSteping )
			ToggleAutomaticSteping();

		SetPlayerPos( World.transform.position );

		World.Initialize( this, Player.transform );
		UI.Initialize( this, World );
		InitializePlayer();
	}

	public void ToggleAutomaticSteping()
	{
		World.ToggleAutomaticSteping();
		UI.AutomaticStepingToggled( World.AutomaticSteping );
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

	public void ReturnToMain()
	{
		if ( World.AutomaticSteping )
			ToggleAutomaticSteping();

		MainController.ReturnToMain();
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
			WorldObject worldObj = hit.transform.parent.GetComponent<WorldObject>();
			if ( worldObj != null )
				cell = worldObj.CurrentCell;
			else
				cell = World.GetClosestCell( hit.point );
		}

		UI.SetCellToDescribe( cell );
	}

	#endregion
}