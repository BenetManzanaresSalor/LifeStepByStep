using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LC_Cell
{
	public Vector3Int TerrainPosition { get; protected set; }

	public LC_Cell(Vector3Int terrainPosition)
	{
		TerrainPosition = terrainPosition;
	}
}