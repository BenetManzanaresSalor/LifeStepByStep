using UnityEngine;

public class FirstPersonController : LC_FirstPersonController
{
	#region Attributes

	#region Settings

	[Header("Clamp position settings")]
	[SerializeField] protected float MaxHeight = 100;

	#endregion

	#region Function

	protected WorldTerrain Terrain;
	protected Vector3 MaxPosition;
	protected Vector3 MinPosition;

	#endregion

	#endregion

	public override void Initialize()
	{
		base.Initialize();

		if ( Terrain == null )
			Terrain = FindObjectOfType<WorldTerrain>();

		Vector3 offset = Terrain.HalfChunk + Terrain.ChunkRenderDistance * Terrain.ChunkSize * Terrain.CellSize;
		MaxPosition = Terrain.transform.position + offset;
		MaxPosition.y = Terrain.transform.position.y + MaxHeight;
		MinPosition = Terrain.transform.position - offset;
		MinPosition.y = Terrain.transform.position.y;
	}

	protected override void Update()
	{
		RotateEnabled = Input.GetMouseButton( 1 );

		base.Update();

		ClampPosition();
	}

	protected virtual void ClampPosition()
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
