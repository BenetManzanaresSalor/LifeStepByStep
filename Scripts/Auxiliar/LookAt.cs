using UnityEngine;

public class LookAt : MonoBehaviour
{
	[SerializeField] protected bool AtCamera = true;
	[SerializeField] protected Transform Target;

	protected virtual void Start()
	{
		if ( AtCamera )
			Target = Camera.main.transform;

		if ( Target == null )
			Debug.LogError( $"LookAt {name} has null target" );
	}

	protected virtual void Update()
	{
		if ( Target != null )
			transform.rotation = Quaternion.LookRotation( transform.position - Target.position );
	}
}
