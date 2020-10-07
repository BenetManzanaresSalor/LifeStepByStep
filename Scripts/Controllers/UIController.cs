using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected TextMeshProUGUI FpsText;
	[SerializeField] protected TextMeshProUGUI StatusText;
	[SerializeField] protected Image PlayPauseIcon;
	[SerializeField] protected Sprite PlaySprite;
	[SerializeField] protected Sprite PauseSprite;

	#endregion

	#endregion

	protected void Update()
	{
		FpsText.text = $"{( 1f / Time.deltaTime ).ToString( "f2" )} FPS";
	}

	public void SetStatus( string status )
	{
		StatusText.text = status;
	}

	public void SetAutomaticSteping( bool enabled )
	{
		PlayPauseIcon.sprite = enabled ? PauseSprite : PlaySprite;
	}
}
