using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Render types for the LC_Map.
/// </summary>
public enum LC_Map_RenderType : int
{
	HEIGHT_DISCRETE = 1,
	HEIGHT_CONTINUOUS = 2
};

/// <summary>
/// Default map class of Lost Cartographer Pack.
/// </summary>
[RequireComponent( typeof( RawImage ) )]
public class LC_Map<Terrain, Chunk, Cell> : LC_GenericMap<Terrain, Chunk, Cell> where Terrain : LC_Terrain<Chunk, Cell> where Chunk : LC_Chunk<Cell> where Cell : LC_Cell
{
	#region Attributes	

	#region Settings

	[Header( "Additional render settings" )]
	[SerializeField]
	[Tooltip( "Render type used at terrain mesh." )]
	protected LC_Map_RenderType RenderType;
	[SerializeField]
	[Tooltip( "Gradient of colors used for map render." )]
	protected Color[] Colors;

	#endregion

	#region Function attributes

	protected RawImage Renderer;

	#endregion

	#endregion

	#region Texture computation

	/// <summary>
	/// Get as reference position the player terrain position.
	/// </summary>
	/// <returns></returns>
	protected override Vector2Int GetReferencePos()
	{
		Vector3Int pos = TerrainToMap.GetReferenceTerrainPos();
		return new Vector2Int( pos.x, pos.z );
	}

	/// <summary>
	/// Computes the color for a cell using the cell height and the Colors array.
	/// </summary>
	/// <param name="cell"></param>
	/// <returns></returns>
	protected override Color32 GetColorPerCell( Cell cell )
	{
		Color32 color;

		if ( cell == null )
			color = Color.black;
		else
		{
			float heightPercentage = Mathf.Clamp( GetHeightPercentage( cell ), 0, 0.99f );
			float colorFloatIndex = heightPercentage * ( Colors.Length - 1 );

			switch ( RenderType )
			{
				case LC_Map_RenderType.HEIGHT_CONTINUOUS:
					int colorIndex = (int)colorFloatIndex;
					float indexDecimals = colorFloatIndex - colorIndex;
					color = ( 1 - indexDecimals ) * Colors[colorIndex] + indexDecimals * Colors[colorIndex + 1];
					break;
				case LC_Map_RenderType.HEIGHT_DISCRETE:
					color = Colors[Mathf.RoundToInt( colorFloatIndex )];
					break;
				default:
					color = Color.black;
					break;
			}
		}

		return color;
	}

	protected virtual float GetHeightPercentage( Cell cell )
	{
		return Mathf.InverseLerp( 0, TerrainToMap.MaxHeight, cell.Height );
	}

	#endregion
}
