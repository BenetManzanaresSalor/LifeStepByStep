using UnityEngine;

public class LookAt : MonoBehaviour
{
	[SerializeField] private bool AtCamera = true;
	[SerializeField] private Transform AlternativeTarget;

	void Start()
	{
		if ( AtCamera )
			AlternativeTarget = FindObjectOfType<Camera>().transform;

		if ( AlternativeTarget == null )
			Debug.LogError( $"LookAt {name} has null target" );
	}

	void Update()
	{
		if ( AlternativeTarget != null )
			transform.LookAt( AlternativeTarget );
	}
}
