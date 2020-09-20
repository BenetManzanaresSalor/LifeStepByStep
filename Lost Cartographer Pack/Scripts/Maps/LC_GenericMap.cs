using UnityEngine;

/// <summary>
/// Generic class parent of any Map of Lost Cartographer Pack.
/// </summary>
public abstract class LC_GenericMap<Terrain, Chunk, Cell> : MonoBehaviour where Terrain : LC_GenericTerrain<Chunk, Cell> where Chunk : LC_Chunk<Cell> where Cell : LC_Cell
{
	#region Attributes

	#region Settings

	[Header( "General settings" )]
	[SerializeField]
	[Tooltip( "Object texture of the map computation." )]
	protected RenderTexture TargetTexture;
	[SerializeField]
	[Tooltip( "Size of the map represented as number of cells." )]
	protected Vector2Int NumCellsWidthAndHeight = new Vector2Int( 512, 512 );
	[SerializeField]
	[Tooltip( "Defines de detail of the map.\nA value of 4 means that the map represents 1 cell of each 4." )]
	protected Vector2Int ResolutionDivider = new Vector2Int( 4, 4 );
	[SerializeField]
	[Tooltip( "Size of the texture to use at TargetTexture." )]
	protected Vector2Int TextureWidthAndHeight = new Vector2Int( 256, 256 );
	[SerializeField]
	[Tooltip( "Number of frames between a update of the map.\nA value of 3 means that the map is updated 1 frame of teach 3." )]
	protected int FramesBtwUpdates = 2;
	[SerializeField]
	[Tooltip( "Maximum seconds for every Update call.\n" +
		"This value is checked between every map cell update, avoiding further updates during that frame if the maximum time is exceeded.\n" +
		"Lower values means better framerate but slower map loading." )]
	protected float MaxUpdateTime = 1f / ( 60f * 2f );
	[SerializeField]
	[Tooltip( "If true, forces the chunk loading needed to render all the cells of the map.\n" +
		"That is, the terrain will load chunks only for map (without Mesh or GameObject) if they are not loaded or loading." )]
	protected bool MapNonLoadedChunks = true;
	[SerializeField]
	[Tooltip( "Use MipMaps at the created texture." )]
	protected bool UseMipMaps = false;

	#endregion

	#region Function attributes

	protected Terrain TerrainToMap;
	protected Vector2Int ReferencePos;
	protected Texture2D MapTexture;
	protected Color32[] TextureColors;
	protected float UpdateIniTime;
	protected Vector2Int HalfMapOffset;
	protected Vector2Int CurrentCellPosInTex;

	#endregion

	#endregion

	#region Initialization

	/// <summary>
	/// Initializes the map variables and the texture.
	/// </summary>
	protected virtual void Start()
	{
		TerrainToMap = FindObjectOfType<Terrain>();
		MapTexture = new Texture2D( TextureWidthAndHeight.x, TextureWidthAndHeight.y );
		TextureColors = new Color32[MapTexture.width * MapTexture.height];
		CurrentCellPosInTex = Vector2Int.zero;
	}

	#endregion

	#region Texture computation

	/// <summary>
	/// Updates the map and the terrain if is required.
	/// </summary>
	protected virtual void Update()
	{
		if ( Time.frameCount % FramesBtwUpdates == 0 )
		{
			UpdateIniTime = Time.realtimeSinceStartup;
			HalfMapOffset = new Vector2Int( NumCellsWidthAndHeight.x / 2, NumCellsWidthAndHeight.y / 2 ); // Update HalfMapOffset for map generation
			ReferencePos = GetReferencePos();

			if ( MapNonLoadedChunks && InMaxUpdateTime() )
				UpdateTerrainToMapChunks();

			if ( InMaxUpdateTime() )
				ComputePixels();
		}
	}

	/// <summary>
	/// Abstract method that gets the reference/central terrain position of the map. Can be the player position.
	/// </summary>
	/// <returns></returns>
	protected abstract Vector2Int GetReferencePos();

	/// <summary>
	/// Checks if the time since the start of the Update method is greater than the MaxUpdateTime.
	/// </summary>
	/// <returns></returns>
	protected virtual bool InMaxUpdateTime()
	{
		return ( Time.realtimeSinceStartup - UpdateIniTime ) <= MaxUpdateTime;
	}

	/// <summary>
	/// <para>Compute the pixels of the map using the terrain cells, continuing from the last cell pixels updated.</para>
	/// <para>For each cell pixels to update it uses the InMaxUpdateTime method, breaking the loop if the MaxUpdateTime is exceeded.</para>
	/// </summary>
	protected virtual void ComputePixels()
	{
		Vector2Int bottomLeftCorner = ReferencePos - HalfMapOffset;
		Vector2Int cellsToGet = NumCellsWidthAndHeight.Div( ResolutionDivider );
		Vector2Int pixelsPerCell = TextureWidthAndHeight.Div( cellsToGet );

		// Compute pixels
		Vector2Int cellPosInTexture = new Vector2Int();
		Vector2Int cellPosInTerrain = new Vector2Int();
		Cell cell;
		Color color;
		int row, column;
		for ( int x = 0; x < cellsToGet.x && InMaxUpdateTime(); x++ )
		{
			for ( int y = 0; y < cellsToGet.y && InMaxUpdateTime(); y++ )
			{
				cellPosInTexture.x = ( CurrentCellPosInTex.x + x ) % cellsToGet.x;
				cellPosInTexture.y = ( CurrentCellPosInTex.y + y ) % cellsToGet.y;
				cellPosInTerrain = bottomLeftCorner + cellPosInTexture * ResolutionDivider;

				cell = TerrainToMap.GetCell( cellPosInTerrain, MapNonLoadedChunks );
				color = GetColorPerCell( cell );
				for ( int i = 0; i < pixelsPerCell.y; i++ )
				{
					row = cellPosInTexture.y * pixelsPerCell.y + i;
					for ( int j = 0; j < pixelsPerCell.x; j++ )
					{
						column = cellPosInTexture.x * pixelsPerCell.x + j;
						TextureColors[row * MapTexture.height + column] = color;
					}
				}
			}
		}

		// Update CurrentCellPosInTex
		CurrentCellPosInTex = cellPosInTexture;

		// Set texture
		MapTexture.SetPixels32( TextureColors );
		MapTexture.Apply();
		Graphics.Blit( MapTexture, TargetTexture );
	}

	/// <summary>
	/// Abstract method that obtains the color that represents a specific cell.
	/// </summary>
	/// <param name="cell">Cell to render.</param>
	/// <returns></returns>
	protected abstract Color32 GetColorPerCell( Cell cell );

	/// <summary>
	/// Updates the terrain chunks for the map.
	/// </summary>
	protected virtual void UpdateTerrainToMapChunks()
	{
		Vector2Int bottomLeftCorner = ReferencePos - HalfMapOffset;
		Vector2Int topRightCorner = ReferencePos + HalfMapOffset;
		TerrainToMap.UpdateChunksForMap( bottomLeftCorner, topRightCorner, InMaxUpdateTime );
	}

	#endregion
}
