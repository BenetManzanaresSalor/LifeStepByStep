using UnityEditor;
using UnityEngine;

public class LC_TerrainInstanciable : LC_Terrain<LC_Chunk<LC_Cell>, LC_Cell>
{
	#region Chunk creation

	protected override LC_Chunk<LC_Cell> CreateChunkInstance( Vector2Int chunkPos )
	{
		return new LC_Chunk<LC_Cell>( chunkPos, ChunkSize );
	}

	/// <summary>
	/// Create a cell of a chunk using the coordinates and the chunk.HeightsMap. 
	/// </summary>
	protected override LC_Cell CreateCell( int chunkX, int chunkZ, LC_Chunk<LC_Cell> chunk )
	{
		return new LC_Cell( new Vector2Int( chunk.CellsOffset.x + chunkX, chunk.CellsOffset.y + chunkZ ),
			chunk.HeightsMap[chunkX + 1, chunkZ + 1] ); // +1 to compensate the offset for normals computation
	}

	#endregion
}
