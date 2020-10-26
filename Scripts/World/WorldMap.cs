using UnityEngine;

public class WorldMap : LC_Map<WorldTerrain, LC_Chunk<WorldCell>, WorldCell>
{
	#region Attributes

	#region Settings

	[Header( "World map" )]
	[SerializeField] protected Color32 EntityColor = Color.blue;
	[SerializeField] protected Color32 FoodColor = Color.green;
	[SerializeField] protected Color32 ObstacleColor = Color.black;

	#endregion

	protected float HalfColorsPercentatge;
	protected float SecondHalfColorsPercentatge;

	#endregion

	protected override void Start()
	{
		int maxColorIdx = Colors.Length - 1;
		HalfColorsPercentatge = ( ( maxColorIdx - 1 ) / 2 ) / (float)maxColorIdx;
		SecondHalfColorsPercentatge = ( ( maxColorIdx + 1 ) / 2 ) / (float)maxColorIdx;

		base.Start();
	}

	protected override Color32 GetColorPerCell( WorldCell cell )
	{
		Color32 color;

		if ( cell != null && cell.Content != null )
		{
			WorldObject content = cell.Content;
			if ( content is Entity )
				color = EntityColor;
			else if ( content is Food )
				color = FoodColor;
			else
				color = ObstacleColor;
		}
		else
			color = base.GetColorPerCell( cell );

		return color;
	}

	protected override float GetHeightPercentage( WorldCell cell )
	{
		float value;

		if ( cell.IsWater )
			value = HalfColorsPercentatge * Mathf.InverseLerp( 0, TerrainToMap.WaterHeight, cell.RealHeight );
		else
			value = SecondHalfColorsPercentatge + HalfColorsPercentatge * Mathf.InverseLerp( TerrainToMap.WaterHeight, TerrainToMap.MaxHeight, cell.Height );

		return value;
	}
}
