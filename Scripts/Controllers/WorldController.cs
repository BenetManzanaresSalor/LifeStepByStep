using UnityEngine;


public class WorldController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected Vector3 PlayerOffset = Vector3.up;

	#endregion

	public GenericWorld CurrentWorld { get; protected set; }
	public FirstPersonController Player { get; protected set; }
	public UIController UI { get; protected set; }

	#endregion

	#region Initialization

	protected void Start()
	{
		CurrentWorld = FindObjectOfType<GenericWorld>();
		Player = FindObjectOfType<FirstPersonController>();
		UI = FindObjectOfType<UIController>();

		ResetWorld();
	}

	#endregion

	#region World control by interaction

	protected void Update()
	{
		if ( Input.GetKeyDown( KeyCode.E ) ) ToggleAutomaticSteping();
		if ( Input.GetKeyDown( KeyCode.R ) ) ResetWorld();
		/*if ( Input.GetKeyDown( KeyCode.F1 ) ) UI.SetStatus( CurrentWorld.GetStatus() );
		if ( Input.GetKeyDown( KeyCode.F2 ) ) CurrentWorld.ResetStatistics();*/
	}

	#endregion

	#region Controls

	protected void ToggleAutomaticSteping()
	{
		CurrentWorld.ToggleAutomaticSteping();
	}

	protected void ResetWorld()
	{
		CurrentWorld.Generate();
		IniPlayerPos();
	}

	protected void IniPlayerPos()
	{
		Player.transform.position = CurrentWorld.GetNearestTerrainRealPos( Player.transform.position ) + PlayerOffset;
	}

	#endregion
}