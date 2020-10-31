using UnityEngine;
using UnityEngine.EventSystems;

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

		if ( pos.x > MaxPosition.x )
			pos.x = MaxPosition.x;
		else if ( pos.x < MinPosition.x )
			pos.x = MinPosition.x;

		if ( pos.y > MaxPosition.y )
			pos.y = MaxPosition.y;
		else if ( pos.y < MinPosition.y )
			pos.y = MinPosition.y;

		if ( pos.z > MaxPosition.z )
			pos.z = MaxPosition.z;
		else if ( pos.z < MinPosition.z )
			pos.z = MinPosition.z;

		transform.position = pos;
	}
}
