public enum WorldCellType : int
{
	GROUND, WATER
}

public enum WorldCellRenderType
{
	HEIGHT, EFFECTORS
}

public static class WorldCellTypeValue
{
	public static bool IsTreadmillable( this WorldCellType type )
	{
		bool isTreadmillable = false;

		switch ( type )
		{
			case WorldCellType.GROUND:
				isTreadmillable = true;
				break;
			case WorldCellType.WATER:
				isTreadmillable = false;
				break;
		}

		return isTreadmillable;
	}

	public static WorldCellType GetTypeByIdentificator( char id, WorldCellType defaultType )
	{
		WorldCellType result = defaultType;

		switch ( id )
		{
			case '_':
				result = WorldCellType.GROUND;
				break;
			case 'w':
				result = WorldCellType.WATER;
				break;
		}

		return result;
	}

}
