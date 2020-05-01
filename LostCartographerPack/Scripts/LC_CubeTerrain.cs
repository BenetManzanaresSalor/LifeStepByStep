using System.Collections.Generic;
using UnityEngine;

public class LC_CubeTerrain : LC_Terrain
{
	#region Attributes

	#region Settings

	[Header( "Cube terrain settings" )]
	[SerializeField] protected bool UseSplitAndMerge;

	#endregion

	#endregion

	#region Render

	protected override void CellsToMesh( LC_Chunk chunk, LC_Cell[,] cells )
	{
		if ( UseSplitAndMerge )
		{
			SplitAndMergeMesh( chunk, cells );
		}
		else
		{
			for ( int x = 0; x < ChunkSize; x++ )
			{
				for ( int z = 0; z < ChunkSize; z++ )
				{
					Vector2Int cellPosInChunk = chunk.CellPosToChunk( cells[x, z].TerrainPos );
					CreateElementMesh( cellPosInChunk, cellPosInChunk, chunk, cells );

					// Create mesh before get the maximum mesh vertices at next cell render
					if ( vertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
					{
						CreateMeshObj( chunk.Obj );
					}
				}
			}
		}
	}

	protected virtual void SplitAndMergeMesh( LC_Chunk chunk, LC_Cell[,] cells )
	{
		List<MathFunctions.QuadTreeSector> sectors = MathFunctions.QuadTree(
			( x, z ) => { return cells[x, z].TerrainPos.y; },
			( x, y ) => { return x == y; },
			ChunkSize, true );

		foreach ( MathFunctions.QuadTreeSector sector in sectors )
		{
			CreateElementMesh( sector.Initial, sector.Final, chunk, cells );

			// Create mesh before get the maximum mesh vertices at next cell render
			if ( vertices.Count + MaxVerticesPerRenderElem >= MaxVerticesByMesh )
			{
				CreateMeshObj( chunk.Obj );
			}
		}
	}

	protected virtual void CreateElementMesh( Vector2Int iniCellPos, Vector2Int endCellPos, LC_Chunk chunk, LC_Cell[,] cells )
	{
		Vector3 realCellPos = ( TerrainPosToReal( cells[iniCellPos.x, iniCellPos.y].TerrainPos ) +
			TerrainPosToReal( cells[endCellPos.x, endCellPos.y].TerrainPos ) ) / 2f;
		int numXCells = endCellPos.x - iniCellPos.x + 1;
		int numZCells = endCellPos.y - iniCellPos.y + 1;

		// Vertices
		vertices.Add( realCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		vertices.Add( realCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		vertices.Add( realCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );
		vertices.Add( realCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );

		// Triangles
		triangles.Add( vertices.Count - 4 );
		triangles.Add( vertices.Count - 1 );
		triangles.Add( vertices.Count - 2 );

		triangles.Add( vertices.Count - 2 );
		triangles.Add( vertices.Count - 3 );
		triangles.Add( vertices.Count - 4 );

		// UVs
		GetUVs( iniCellPos, out Vector2 iniUV, out Vector2 endUV, chunk, cells );
		uvs.Add( new Vector2( iniUV.x, endUV.y ) );
		uvs.Add( new Vector2( endUV.x, endUV.y ) );
		uvs.Add( new Vector2( endUV.x, iniUV.y ) );
		uvs.Add( new Vector2( iniUV.x, iniUV.y ) );

		// Positive x border
		if ( endCellPos.x < cells.GetLength( 0 ) - 1 )
		{
			for ( int z = 0; z < numZCells; z++ )
			{
				realCellPos = TerrainPosToReal( cells[endCellPos.x, endCellPos.y - z].TerrainPos );
				CreateEdgeMesh( realCellPos, cells[endCellPos.x + 1, endCellPos.y - z], true, iniUV, endUV, chunk, cells );
			}
		}

		// Positive z border
		if ( endCellPos.y < cells.GetLength( 1 ) - 1 )
		{
			for ( int x = 0; x < numXCells; x++ )
			{
				realCellPos = TerrainPosToReal( cells[endCellPos.x - x, endCellPos.y].TerrainPos );
				CreateEdgeMesh( realCellPos, cells[endCellPos.x - x, endCellPos.y + 1], false, iniUV, endUV, chunk, cells );
			}
		}
	}

	protected virtual void CreateEdgeMesh( Vector3 cellRealPos, LC_Cell edgeCell, bool toRight, Vector2 iniUV, Vector2 endUV, LC_Chunk chunk, LC_Cell[,] cells )
	{
		Vector2 edgeIniUV;
		Vector2 edgeEndUV;
		float edgeCellHeightDiff = TerrainPosToReal( edgeCell.TerrainPos ).y - cellRealPos.y;

		if ( edgeCellHeightDiff != 0 )
		{
			float xMultipler = 1;
			float zMultipler = -1;
			if ( !toRight )
			{
				xMultipler = -1;
				zMultipler = 1;
			}

			// Set edge vertexs
			vertices.Add( cellRealPos + new Vector3( CellSize.x * xMultipler / 2f, edgeCellHeightDiff, CellSize.z * zMultipler / 2f ) );
			vertices.Add( cellRealPos + new Vector3( CellSize.x / 2f, edgeCellHeightDiff, CellSize.z / 2f ) );
			vertices.Add( cellRealPos + new Vector3( CellSize.x / 2f, 0, CellSize.z / 2f ) );
			vertices.Add( cellRealPos + new Vector3( CellSize.x * xMultipler / 2f, 0, CellSize.z * zMultipler / 2f ) );

			// Set edge triangles
			if ( toRight )
			{
				triangles.Add( vertices.Count - 4 );
				triangles.Add( vertices.Count - 1 );
				triangles.Add( vertices.Count - 2 );

				triangles.Add( vertices.Count - 2 );
				triangles.Add( vertices.Count - 3 );
				triangles.Add( vertices.Count - 4 );
			}
			// Inverted ( needed to be seen )
			else
			{
				triangles.Add( vertices.Count - 2 );
				triangles.Add( vertices.Count - 1 );
				triangles.Add( vertices.Count - 4 );

				triangles.Add( vertices.Count - 4 );
				triangles.Add( vertices.Count - 3 );
				triangles.Add( vertices.Count - 2 );
			}

			// Set edge UVs dependently of the height difference
			if ( edgeCellHeightDiff < 0 )
			{
				edgeIniUV = iniUV;
				edgeEndUV = endUV;
			}
			else
			{
				Vector2Int cellPosInChunk = new Vector2Int( edgeCell.TerrainPos.x - chunk.CellsOffset.x, edgeCell.TerrainPos.z - chunk.CellsOffset.y );
				GetUVs( cellPosInChunk, out edgeIniUV, out edgeEndUV, chunk, cells );
			}
			uvs.Add( new Vector2( edgeIniUV.x, edgeEndUV.y ) );
			uvs.Add( new Vector2( edgeEndUV.x, edgeEndUV.y ) );
			uvs.Add( new Vector2( edgeEndUV.x, edgeIniUV.y ) );
			uvs.Add( new Vector2( edgeIniUV.x, edgeIniUV.y ) );
		}
	}

	#endregion
}
