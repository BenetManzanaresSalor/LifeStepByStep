using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// <para>Controls the player body movement and camera rotation. It is used by MainController and GameController.</para>
/// <para>In the main menu the movement is disabled, only using the camera.</para>
/// <para>In the game, movement and camera are enabled, limiting movement to terrain space and colliding with ground and objects.Also, to rotate the camera, you must press the right mouse button.</para>
/// <para>Additionally, if the player uses the left click raycasting will be used to select a cell, calling the GameController's SelectCell method with collision info as argument.</para>
/// </summary>
public class FirstPersonController : LC_FirstPersonController
{
	#region Attributes

	#region Settings

	[Header( "Clamp position settings" )]
	[SerializeField] protected float MaxOffsetHeight = 50;

	#endregion

	#region Function

	protected GameController GameController;
	protected Vector3 MinPosition;
	protected Vector3 MaxPosition;

	#endregion

	#endregion

	public void Initialize( GameController gameController )
	{
		base.Initialize();

		GameController = gameController;

		GameController.GetTerrainLimits( out MinPosition, out MaxPosition );
		MaxPosition.y += MaxOffsetHeight;
	}

	protected override void Update()
	{
		RotateEnabled = Input.GetMouseButton( 1 );

		if ( Input.GetMouseButtonDown( 0 ) )
			SelectCell();

		base.Update();

		ClampPosition();
	}

	protected void SelectCell()
	{
		if ( GameController != null && !EventSystem.current.IsPointerOverGameObject() )
		{
			Vector3 mousePosition = Input.mousePosition;
			mousePosition.z = Camera.main.nearClipPlane;

			Ray ray = Camera.main.ScreenPointToRay( mousePosition );
			bool isCollision = Physics.Raycast( ray, out RaycastHit hit, 1000 );
			GameController.SelectCell( isCollision, hit );
		}
	}

	protected void ClampPosition()
	{
		Vector3 pos = transform.position;

		pos.x = LC_Math.Clamp( pos.x, MinPosition.x, MaxPosition.x );
		pos.y = LC_Math.Clamp( pos.y, MinPosition.y, MaxPosition.y );
		pos.z = LC_Math.Clamp( pos.z, MinPosition.z, MaxPosition.z );

		transform.position = pos;
	}
}
