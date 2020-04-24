using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LC_RenderType : int
{
	HEIGHT,
	SMOOTHING
}

public struct LC_Chunk
{
	public GameObject Obj;
	public Vector2Int CellsOffset;

	public LC_Chunk( GameObject obj, Vector2Int cellsOffset )
	{
		Obj = obj;
		CellsOffset = cellsOffset;
	}

	public Vector2Int CellPosToChunk( Vector3Int cellPos )
	{
		return new Vector2Int( cellPos.x - CellsOffset.x, cellPos.z - CellsOffset.y );
	}

	public Vector2Int CellPosToChunk( Vector2Int cellPos )
	{
		return cellPos - CellsOffset;
	}
}