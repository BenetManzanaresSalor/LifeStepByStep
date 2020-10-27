using UnityEngine;

public class MainUI : MonoBehaviour
{
	#region Attributes

	#region Function

	protected MainController MainController;

	#endregion

	#endregion

	#region Initialization

	public void Initialize( MainController mainController )
	{
		MainController = mainController;
	}

	#endregion

	public void PlayGame() => MainController.PlayGame();

	public void ExitGame() => MainController.ExitGame();
}
