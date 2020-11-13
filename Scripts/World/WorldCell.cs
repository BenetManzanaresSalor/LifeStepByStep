using UnityEngine;

/// <summary>
/// <para>Defines each cell of WorldTerrain.</para>
/// <para>Controlled by WorldTerrain.</para>
/// </summary>
public class WorldCell : LC_Cell
{
	#region Attributes

	public WorldObject Content { get; protected set; }
	public float RealHeight { get; protected set; }
	public bool IsWater { get; protected set; }

	#endregion

	public WorldCell( Vector2Int terrainPosition, float renderHeight, float realHeight, bool isWater ) : base( terrainPosition, renderHeight )
	{
		RealHeight = realHeight;
		IsWater = isWater;
	}

	public bool IsFree()
	{
		return !IsWater && Content == null;
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