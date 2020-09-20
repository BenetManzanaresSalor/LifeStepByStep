using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotation : MonoBehaviour
{
	[SerializeField] private Vector2 MinAndMaxRange = new Vector2( 0, 359 );
	[SerializeField] private bool InX = false;
	[SerializeField] private bool InY = true;
	[SerializeField] private bool InZ = false;

	void Start()
    {
		float value = MinAndMaxRange.x + Random.value * ( MinAndMaxRange.y - MinAndMaxRange.x );
		transform.rotation *= Quaternion.Euler( InX ? value : 0, InY ? value : 0, InZ ? value : 0 );
    }
}
