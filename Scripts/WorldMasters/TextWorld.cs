using System;
using System.IO;
using UnityEngine;

public class TextWorld : WorldMaster
{
	#region Attributes

	#region Settings

	[Header( "Text world settings" )]
	[SerializeField] protected string MapDefinitionFile;
	[SerializeField] protected char LinesSeparator = '\n';
	[SerializeField] protected char PositionsSeparator = ' ';
	[SerializeField] protected char ContentsSeparator = ',';

	#endregion

	protected string[,] Map;

	#endregion

	#region Initialzation

	protected override void CreateMap()
	{
		try
		{
			using ( StreamReader reader = new StreamReader( @"Assets/Resources/" + MapDefinitionFile ) )
			{
				string[] lines = reader.ReadToEnd().Split( LinesSeparator );
				string[] line;

				// Search max columns
				int maxColumns = 0;
				for ( int z = 0; z < lines.Length; z++ )
				{
					line = lines[z].Split( PositionsSeparator );

					if ( maxColumns < line.Length )
					{
						maxColumns = line.Length;
					}
				}

				// Define map and world size
				Xsize = maxColumns;
				Zsize = lines.Length;

				// Create map
				Map = new string[Xsize, Zsize];

				for ( int z = 0; z < lines.Length; z++ )
				{
					line = lines[z].Split( PositionsSeparator );

					for ( int x = 0; x < line.Length; x++ )
					{
						Map[x, z] = line[x];
					}
				}
			}
		}
		catch ( Exception exc )
		{
			UnityEngine.Debug.LogError( "Error in map parse " + exc );
		}
	}

	protected override WorldCell CreateWorldCell( int x, int z )
	{
		Vector3Int WorldPosition3D = new Vector3Int( x, 0, z );
		WorldCellType type = DefaultWorldCellType;
		WorldObject worldObj = null;
		WorldObject content = null;

		if ( Map[x, z] != null || Map[x, z] == string.Empty )
		{
			foreach ( char elementID in Map[x, z].ToCharArray() )
			{
				type = WorldCellTypeValue.GetTypeByIdentificator( elementID, DefaultWorldCellType );

				// If maybe is not a type id
				if ( type == DefaultWorldCellType )
				{
					worldObj = WorldObjectById( elementID );

					// If is WorldObject
					if ( worldObj != null && content == null )
					{
						content = Instantiate( worldObj, Vector3.zero, Quaternion.identity, this.transform );
					}
				}
			}

			// Set height
			WorldPosition3D.y = type == WorldCellType.GROUND ? MinAndMaxHeights.y : MinAndMaxHeights.x;
		}

		return new WorldCell( this, WorldPosition3D, type, content );
	}

	#endregion

	#region Auxiliar

	protected WorldObject WorldObjectById( char id )
	{
		WorldObject result = null;
		WorldObject possibleResult = null;

		for ( int pos = 0; pos < WorldObjects.Length && result == null; pos++ )
		{
			possibleResult = WorldObjects[pos];
			if ( possibleResult.Identificator == id )
			{
				result = possibleResult;
			}
		}

		return result;
	}

	#endregion
}
