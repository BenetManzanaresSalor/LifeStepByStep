using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LC_CubeTerrainInstanciable : LC_CubeTerrain<LC_Chunk<LC_Cell>, LC_Cell>
{
	#region Chunk creation

	public void Start()
	{
		Generate();
	}

	protected override LC_Chunk<LC_Cell> CreateChunkInstance( Vector2Int chunkPos )
	{
		return new LC_Chunk<LC_Cell>( chunkPos, ChunkSize );
	}

	protected override LC_Cell CreateCell( int chunkX, int chunkZ, LC_Chunk<LC_Cell> chunk )
	{
		LC_Cell cell = new LC_Cell( new Vector2Int( chunk.CellsOffset.x + chunkX, chunk.CellsOffset.y + chunkZ ),
			chunk.HeightsMap[chunkX + 1, chunkZ + 1] ); // +1 to compensate the offset for normals computation
		cell.Height = Mathf.RoundToInt( cell.Height );
		return cell;
	}

	#endregion
}