using UnityEngine;

public class WorldController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected Vector3 PlayerOffset = Vector3.up;

	#endregion

	public WorldMaster CurrentWorld { get; protected set; }
	public FirstPersonController Player { get; protected set; }
	public UIController UI { get; protected set; }

	#endregion

	#region Initialization

	protected void Start()
	{
		CurrentWorld = FindObjectOfType<WorldMaster>();
		Player = FindObjectOfType<FirstPersonController>();
		UI = FindObjectOfType<UIController>();

		ResetWorld();
	}

	protected void ResetWorld()
	{
		CurrentWorld.ResetWorld();
		IniPlayerPos();
	}

	protected void IniPlayerPos()
	{
		Player.transform.position = CurrentWorld.GetIniCellPos() + PlayerOffset;
	}

	#endregion

	#region World control by interaction

	protected void Update()
	{
		if ( Input.GetKeyDown( KeyCode.E ) ) CurrentWorld.SetAutomaticSteps();
		if ( Input.GetKeyDown( KeyCode.R ) ) ResetWorld();		
		if ( Input.GetKeyDown( KeyCode.F1 ) ) UI.SetStatus( CurrentWorld.GetStatus() );
		if ( Input.GetKeyDown( KeyCode.F2 ) ) CurrentWorld.ResetStatistics();
	}

	#endregion
}