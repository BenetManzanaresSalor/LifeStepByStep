using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected TextMeshProUGUI StatusText;
	[SerializeField] protected TextMeshProUGUI FpsText;

	#endregion

	#endregion

	protected void Update()
	{
		FpsText.text = $"FPS = {1f / Time.deltaTime}";
	}

	public void SetStatus( string status )
	{
		StatusText.text = status;
	}
}
