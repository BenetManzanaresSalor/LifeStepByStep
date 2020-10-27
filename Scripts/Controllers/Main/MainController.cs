using UnityEngine;

public class MainController : MonoBehaviour
{
	#region Attributes

	#region Settings

	[SerializeField] protected FirstPersonController Player;
	[SerializeField] protected MainUI UI;
	[SerializeField] protected GameController GameController;

	#endregion

	#region Function

	protected bool IsGameStarted = false;

	#endregion

	#endregion

	#region Initialization

	public void Start()
	{
		UI.gameObject.SetActive( true );
		UI.Initialize( this );

		GameController.gameObject.SetActive( false );
	}

	#endregion

	public void PlayGame()
	{
		UI.gameObject.SetActive( false );
		
		if ( !IsGameStarted )
		{
			GameController.gameObject.SetActive( true );
			GameController.Initialize( this );
			IsGameStarted = true;
		}
		GameController.SetEnabled( true );

		Player.enabled = true;
	}

	public void ReturnToMain()
	{
		GameController.SetEnabled( false );
		UI.gameObject.SetActive( true );
		Player.enabled = false;		
	}

	public void ExitGame()
	{
		Debug.Log( "Exit" ); // TODO : Delete this
		Application.Quit();
	}
}
