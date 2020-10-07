using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modification of the LC_Terrain class that applies a cube shape to each cell.
/// </summary>
public abstract class LC_CubeTerrain<Chunk, Cell> : LC_Terrain<Chunk, Cell> where Chunk : LC_Chunk<Cell> where Cell : LC_Cell
{
	#region Attributes

	#region Settings

	[Header( "Cube terrain settings" )]
	[SerializeField]
	[Tooltip( "If true, uses split and merge algorithm with cells with same height.\nThis simplfy the mesh, improving the performance.\nAffects to texture UVs." )]
	protected bool UseSplitAndMerge;

	#endregion

	#endregion

	#region Chunk creation

	protected abstract override Cell CreateCell( int chunkX, int chunkZ, Chunk chunk );

	#endregion

	#region Mesh

	/// <summary>
	/// Compute the mesh using Split and Merge algorithm if is specified.
	/// </summary>
	/// <param name="chunk"></param>
	protected override void ComputeMesh( Chunk chunk )
	{
		if ( UseSplitAndMerge )
		{
			SplitAndMergeMesh( chunk );
		}
		else
		{
			for ( int x = 0; x < ChunkSize; x++ )
			{
				for ( int z = 0; z < ChunkSize; z++ )
				{
					Vector2Int cellPosInChunk = chunk.TerrainPosToChunk( chunk.Cells[x, z].TerrainPos );
					CreateElementMesh( cellPosInChunk, cellPosInChunk, chunk );
				}
			}
		}
	}

	/// <summary>
	/// Applies the Split and Merge algorithm and create the mesh for the result sectors.
	/// </summary>
	/// <param name="chunk"></param>
	protected virtual void SplitAndMergeMesh( Chunk chunk )
	{
		List<LC_Math.QuadTreeSector> sectors = LC_Math.SplitAndMerge(
			( x, z ) => { return chunk.Cells[x, z].Height; },
			( x, y ) => { return x == y; },
			ChunkSize, true );

		foreach ( LC_Math.QuadTreeSector sector in sectors )
			CreateElementMesh( sector.Initial, sector.Final, chunk );
	}

	/// <summary>
	/// Compute the cube mesh of a element (cell or sector of cells) and add it to the chunk mesh data.
	/// </summary>
	/// <param name="iniCellPos"></param>
	/// <param name="endCellPos"></param>
	/// <param name="chunk"></param>
	protected virtual void CreateElementMesh( Vector2Int iniCellPos, Vector2Int endCellPos, Chunk chunk )
	{
		Vector3 realCellPos = ( TerrainPosToReal( chunk.Cells[iniCellPos.x, iniCellPos.y] ) +
			TerrainPosToReal( chunk.Cells[endCellPos.x, endCellPos.y] ) ) / 2f;

		int numXCells = endCellPos.x - iniCellPos.x + 1;
		int numZCells = endCellPos.y - iniCellPos.y + 1;

		// Vertices
		chunk.Vertices.Add( realCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		chunk.Vertices.Add( realCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, -CellSize.z * numZCells / 2f ) );
		chunk.Vertices.Add( realCellPos + new Vector3( CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );
		chunk.Vertices.Add( realCellPos + new Vector3( -CellSize.x * numXCells / 2f, 0, CellSize.z * numZCells / 2f ) );

		// Triangles
		chunk.Triangles.Add( chunk.Vertices.Count - 4 );
		chunk.Triangles.Add( chunk.Vertices.Count - 1 );
		chunk.Triangles.Add( chunk.Vertices.Count - 2 );

		chunk.Triangles.Add( chunk.Vertices.Count - 2 );
		chunk.Triangles.Add( chunk.Vertices.Count - 3 );
		chunk.Triangles.Add( chunk.Vertices.Count - 4 );

		// UVs
		GetUVs( iniCellPos, out Vector2 iniUV, out Vector2 endUV, chunk );
		chunk.UVs.Add( new Vector2( iniUV.x, endUV.y ) );
		chunk.UVs.Add( new Vector2( endUV.x, endUV.y ) );
		chunk.UVs.Add( new Vector2( endUV.x, iniUV.y ) );
		chunk.UVs.Add( new Vector2( iniUV.x, iniUV.y ) );

		// Positive x border
		if ( endCellPos.x < chunk.Cells.GetLength( 0 ) - 1 )
		{
			for ( int z = 0; z < numZCells; z++ )
			{
				realCellPos = TerrainPosToReal( chunk.Cells[endCellPos.x, endCellPos.y - z] );
				CreateEdgeMesh( realCellPos, chunk.Cells[endCellPos.x + 1, endCellPos.y - z], true, iniUV, endUV, chunk );
			}
		}

		// Positive z border
		if ( endCellPos.y < chunk.Cells.GetLength( 1 ) - 1 )
		{
			for ( int x = 0; x < numXCells; x++ )
			{
				realCellPos = TerrainPosToReal( chunk.Cells[endCellPos.x - x, endCellPos.y] );
				CreateEdgeMesh( realCellPos, chunk.Cells[endCellPos.x - x, endCellPos.y + 1], false, iniUV, endUV, chunk );
			}
		}
	}

	/// <summary>
	/// Create the mesh of a edge between one real pos and a edge cell, adding the result to the chunk mesh data.
	/// </summary>
	/// <param name="cellRealPos"></param>
	/// <param name="edgeCell"></param>
	/// <param name="toRight">If the mesh is at right side.</param>
	/// <param name="iniUV"></param>
	/// <param name="endUV"></param>
	/// <param name="chunk"></param>
	protected virtual void CreateEdgeMesh( Vector3 cellRealPos, Cell edgeCell, bool toRight, Vector2 iniUV, Vector2 endUV, Chunk chunk )
	{
		Vector2 edgeIniUV;
		Vector2 edgeEndUV;
		float edgeCellHeightDiff = TerrainPosToReal( edgeCell ).y - cellRealPos.y;

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
			chunk.Vertices.Add( cellRealPos + new Vector3( CellSize.x * xMultipler / 2f, edgeCellHeightDiff, CellSize.z * zMultipler / 2f ) );
			chunk.Vertices.Add( cellRealPos + new Vector3( CellSize.x / 2f, edgeCellHeightDiff, CellSize.z / 2f ) );
			chunk.Vertices.Add( cellRealPos + new Vector3( CellSize.x / 2f, 0, CellSize.z / 2f ) );
			chunk.Vertices.Add( cellRealPos + new Vector3( CellSize.x * xMultipler / 2f, 0, CellSize.z * zMultipler / 2f ) );

			// Set edge triangles
			if ( toRight )
			{
				chunk.Triangles.Add( chunk.Vertices.Count - 4 );
				chunk.Triangles.Add( chunk.Vertices.Count - 1 );
				chunk.Triangles.Add( chunk.Vertices.Count - 2 );

				chunk.Triangles.Add( chunk.Vertices.Count - 2 );
				chunk.Triangles.Add( chunk.Vertices.Count - 3 );
				chunk.Triangles.Add( chunk.Vertices.Count - 4 );
			}
			// Inverted ( needed to be seen )
			else
			{
				chunk.Triangles.Add( chunk.Vertices.Count - 2 );
				chunk.Triangles.Add( chunk.Vertices.Count - 1 );
				chunk.Triangles.Add( chunk.Vertices.Count - 4 );

				chunk.Triangles.Add( chunk.Vertices.Count - 4 );
				chunk.Triangles.Add( chunk.Vertices.Count - 3 );
				chunk.Triangles.Add( chunk.Vertices.Count - 2 );
			}

			// Set edge UVs dependently of the height difference
			if ( edgeCellHeightDiff < 0 )
			{
				edgeIniUV = iniUV;
				edgeEndUV = endUV;
			}
			else
			{
				GetUVs( chunk.TerrainPosToChunk( edgeCell.TerrainPos ),
					out edgeIniUV, out edgeEndUV, chunk );
			}
			chunk.UVs.Add( new Vector2( edgeIniUV.x, edgeEndUV.y ) );
			chunk.UVs.Add( new Vector2( edgeEndUV.x, edgeEndUV.y ) );
			chunk.UVs.Add( new Vector2( edgeEndUV.x, edgeIniUV.y ) );
			chunk.UVs.Add( new Vector2( edgeIniUV.x, edgeIniUV.y ) );
		}
	}
	
	#endregion
}
