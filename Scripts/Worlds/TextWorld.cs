using System;
using System.IO;
using UnityEngine;

public class TextWorld : GenericWorld
{
	#region Attributes

	#region Settings

	[Header( "Text world settings" )]
	[SerializeField] protected string TerrainDefinitionFile;
	[SerializeField] protected char LinesSeparator = '\n';
	[SerializeField] protected char PositionsSeparator = ' ';
	[SerializeField] protected char ContentsSeparator = ',';

	#endregion

	// TODO : Cancel random terrain generation

	#region Function attributes
	
	protected string[,] TerrainMap;

	#endregion

	#endregion

	#region Initialzation

	public override void Generate()
	{
		CreateMap();
		base.Generate();
	}

	protected virtual void CreateMap()
	{
		try
		{
			using ( StreamReader reader = new StreamReader( @"Assets/Resources/" + TerrainDefinitionFile ) )
			{
				string[] lines = reader.ReadToEnd().Split( LinesSeparator );
				string[] line;

				// Search max columns
				int maxColumns = 0;
				for ( int z = 0; z < lines.Length; z++ )
				{
					line = lines[z].Split( PositionsSeparator );

					if ( maxColumns < line.Length )
						maxColumns = line.Length;
				}

				// Define map and world size
				// TODO
				int Xsize = maxColumns;
				int Zsize = lines.Length;

				// Create map
				TerrainMap = new string[Xsize, Zsize];
				for ( int z = 0; z < lines.Length; z++ )
				{
					line = lines[z].Split( PositionsSeparator );

					for ( int x = 0; x < line.Length; x++ )
						TerrainMap[x, z] = line[x];
				}
			}
		}
		catch ( Exception exc )
		{
			UnityEngine.Debug.LogError( "Error in map parse " + exc );
		}
	}
	
	public override WorldObject CreateWorldObject( WorldCell cell )
	{
		WorldObject res = null;

		Vector2Int pos = cell.TerrainPos;
		if ( pos.x >= 0 && pos.x < TerrainMap.GetLength( 0 ) && pos.y >= 0 && pos.y < TerrainMap.GetLength( 1 ) )
		{
			char[] cellString = TerrainMap[pos.x, pos.y].ToCharArray();
			for ( int i = 0; i < cellString.Length && res != null; i++ )
				res = WorldObjectById( cellString[i] );
		}

		return res;
	}

	protected WorldObject WorldObjectById( char id )
	{
		WorldObject result = null;

		for ( int i = 0; i < WorldObjects.Length && result == null; i++ )
		{
			if ( WorldObjects[i].Identificator == id )
				result = WorldObjects[i];
		}

		return result;
	}

	#endregion
}