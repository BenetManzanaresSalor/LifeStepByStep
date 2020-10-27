using UnityEngine;

/// <summary>
/// Auxiliar class for the Lost Cartographer Pack examples.
/// </summary>
[RequireComponent( typeof( CharacterController ) )]
public class LC_FirstPersonController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[Header( "Global settings" )]
	[SerializeField] public bool AutoInitialize = true;
	[SerializeField] public bool MoveEnabled = true;
	[SerializeField] public bool RotateEnabled = true;

	[SerializeField] public float MouseSensitivity = 100f;
	[SerializeField] public float HoritzontalVelocity = 10f;
	[SerializeField] public bool FreeVerticalMovement = false;
	[SerializeField] public float FreeVerticalVelocity = 15f;

	[Header( "Jump settings" )]
	[SerializeField] public float JumpHeight = 3f;
	[SerializeField] public bool UseMoreRealisticJump = true;
	[SerializeField] public float MoreRealisticJumpVelocityDivisor = 2f;
	[SerializeField] public Transform SphericGroundCheck;
	[SerializeField] public LayerMask WhatIsGround;

	#endregion

	#region Function attributes

	protected bool IsInitialized = false;
	protected CharacterController Controller;
	protected Camera PlayerCamera;
	protected Transform CameraTransform { get => PlayerCamera.transform; }
	protected Vector3 CurrentVelocity;
	public float VerticalRotationRange = 180f;
	public const float Gravity = -9.807f;
	protected bool IsGrounded;
	protected Vector2 VelocityBeforeJump;

	#endregion

	#endregion

	#region Initialization

	protected virtual void Start()
	{
		if ( AutoInitialize )
			Initialize();
	}

	public virtual void Initialize()
	{
		IsInitialized = true;

		if ( Controller == null )
			Controller = GetComponent<CharacterController>();
		if ( PlayerCamera == null )
			PlayerCamera = GetComponentInChildren<Camera>();
	}

	#endregion

	#region Movement

	protected virtual void Update()
	{
		if ( IsInitialized )
		{
			IsGrounded = Physics.CheckSphere( SphericGroundCheck.position, SphericGroundCheck.lossyScale.x, WhatIsGround );

			if ( RotateEnabled )
				Rotate( ComputeRotation() );

			if ( MoveEnabled )
				Move( ComputeVelocity() );
		}
	}

	protected virtual Vector3 ComputeRotation()
	{
		Vector3 newRotation = Vector3.zero;
		newRotation.x = Input.GetAxis( "Mouse Y" ) * MouseSensitivity * Time.deltaTime;
		newRotation.y = Input.GetAxis( "Mouse X" ) * MouseSensitivity * Time.deltaTime;

		return newRotation;
	}

	protected virtual void Rotate( Vector3 newRotation )
	{
		// Rotate at X
		Vector3 cameraRotation = CameraTransform.localEulerAngles;
		cameraRotation.x = ApplyRotationRange( cameraRotation.x - newRotation.x, VerticalRotationRange );
		CameraTransform.localEulerAngles = cameraRotation;

		// Rotate at Y
		transform.Rotate( Vector3.up * newRotation.y );
	}

	protected virtual Vector3 ComputeVelocity()
	{
		Vector3 newVelocity = Vector3.zero;

		Vector2 planeVelocity = Vector2.zero;
		Vector3 direction3D = transform.right * Input.GetAxis( "Horizontal" ) + transform.forward * Input.GetAxis( "Vertical" );
		Vector2 direction2D = new Vector2( direction3D.x, direction3D.z );

		if ( IsGrounded || !UseMoreRealisticJump )
		{
			planeVelocity = direction2D * HoritzontalVelocity;
		}
		else
		{
			float scalarProduct = Vector2.Dot( direction2D, VelocityBeforeJump.normalized );
			planeVelocity = direction2D * scalarProduct * HoritzontalVelocity +
				direction2D * ( ( 1f - scalarProduct ) * HoritzontalVelocity / MoreRealisticJumpVelocityDivisor );
		}

		newVelocity.x = planeVelocity.x;
		newVelocity.y = ComputeVerticalVelocity();
		newVelocity.z = planeVelocity.y;

		return newVelocity;
	}

	protected virtual float ComputeVerticalVelocity()
	{
		float velocity = CurrentVelocity.y;

		if ( FreeVerticalMovement )
		{
			velocity = Input.GetAxis( "Jump" ) * FreeVerticalVelocity;    // Check up
			if ( velocity == 0 && Input.GetKey( KeyCode.LeftShift ) )  // Check down
				velocity = -FreeVerticalVelocity;
		}
		else
		{
			if ( IsGrounded )
			{
				if ( velocity < 0 )
					velocity = Gravity / 3f;    // When is grounded, force to touch ground (the ground check has a radius)

				if ( Input.GetButtonDown( "Jump" ) )
				{
					VelocityBeforeJump = new Vector2( CurrentVelocity.x, CurrentVelocity.z );
					velocity = Mathf.Sqrt( JumpHeight * -2f * Gravity );
				}
			}
			else
			{
				velocity += Gravity * Time.deltaTime;
			}
		}

		return velocity;
	}

	protected virtual void Move( Vector3 velocity )
	{
		CurrentVelocity = velocity;
		Controller.Move( CurrentVelocity * Time.deltaTime );
	}

	#endregion

	#region Auxiliar

	public virtual float ApplyRotationRange( float rotation, float rotationRange )
	{
		rotation = Mod( rotation, 360 );
		float topAngle = Mod( -VerticalRotationRange / 2f, 360 );
		float bottomAngle = VerticalRotationRange / 2f;
		if ( rotation > bottomAngle && rotation < topAngle )
		{
			float disToBottom = rotation - bottomAngle;
			float disToTop = topAngle - rotation;
			rotation = ( disToBottom < disToTop ) ? bottomAngle : topAngle;
		}

		return rotation;
	}

	public static float Mod( float x, float m )
	{
		float r = x % m;
		return r < 0 ? r + m : r;
	}

	#endregion

	#region External use

	public virtual RaycastHit Raycast( float raycastingRange, LayerMask raycastingMask )
	{
		Physics.Raycast( CameraTransform.position, CameraTransform.forward, out RaycastHit hit, raycastingRange, raycastingMask, QueryTriggerInteraction.Collide );
		return hit;
	}

	#endregion
}
