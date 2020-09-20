using UnityEngine;

public class WorldCell : LC_Cell
{
	#region Attributes

	public WorldObject Content { get; protected set; }

	#endregion

	public WorldCell( Vector2Int terrainPosition, float height ) : base( terrainPosition, height ) { }

	public bool IsFree()
	{
		return Content == null;
	}

	public bool TrySetContent( WorldObject content )
	{
		bool canSetContent = IsFree();

		if ( canSetContent )
			Content = content;

		return canSetContent;
	}

	public void DeleteContent()
	{
		Content = null;
	}
}