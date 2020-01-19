using UnityEngine;

[RequireComponent( typeof( CharacterController ) )]
public class FirstPersonController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected float MouseSensitivity = 100f;
	[SerializeField] protected float XandZvelocity = 15f;	
	[SerializeField] protected float JumpHeight = 3f;
	[SerializeField] protected float Gravity = -9.807f;
	[SerializeField] protected Transform GroundCheck;
	[SerializeField] protected float GroundCheckRadius = 1f;
	[SerializeField] protected LayerMask WhatIsGround;
	[SerializeField] protected bool FreeVerticalMovement = false;
	[SerializeField] protected float Yvelocity = 15f;

	#endregion

	protected Transform CameraTransform;
	protected float XRotation;

	protected CharacterController Controller;
	protected float VerticalVelocity;
	protected bool IsGrounded;	

	#endregion

	void Start()
	{
		CameraTransform = Camera.main.transform;
		XRotation = 0;

		Controller = GetComponent<CharacterController>();
		VerticalVelocity = FreeVerticalMovement ? 0 : Gravity;
		IsGrounded = false;		
	}

	void Update()
	{
		// Camera movement
		float mouseX = Input.GetAxis( "Mouse X" ) * Time.deltaTime * MouseSensitivity;
		float mouseY = Input.GetAxis( "Mouse Y" ) * Time.deltaTime * MouseSensitivity;

		XRotation = MathFunctions.Clamp( XRotation - mouseY, -90f, 90f );

		CameraTransform.localRotation = Quaternion.Euler( XRotation, 0f, 0f );
		transform.Rotate( Vector3.up * mouseX );

		// X-Z movement
		Controller.Move( ( transform.right * Input.GetAxis( "Horizontal" ) + transform.forward * Input.GetAxis( "Vertical" ) ) *
			XandZvelocity * Time.deltaTime );

		// Y movement
		if ( FreeVerticalMovement )
		{
			VerticalVelocity = Input.GetAxis( "Jump" ) * Yvelocity; // Check up
			if ( VerticalVelocity == 0 && Input.GetKey( KeyCode.LeftShift ) ) // Check down
			{
				VerticalVelocity = -Yvelocity;
			}
		}
		else
		{
			IsGrounded = Physics.CheckSphere( GroundCheck.position, GroundCheckRadius, WhatIsGround );
			if ( IsGrounded )
			{
				if ( VerticalVelocity < 0 )
					VerticalVelocity = -2f;

				if ( Input.GetButtonDown( "Jump" ) )
					VerticalVelocity = Mathf.Sqrt( JumpHeight * -2f * Gravity );
			}

			VerticalVelocity += Gravity * Time.deltaTime;
		}

		Controller.Move( Vector3.up * VerticalVelocity * Time.deltaTime );
	}
}
