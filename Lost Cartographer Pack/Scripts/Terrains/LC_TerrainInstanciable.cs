using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LC_TerrainInstanciable : LC_Terrain<LC_Chunk<LC_Cell>,LC_Cell>
{
	#region Settings

	[SerializeField]
	[Tooltip( "If true, regenerates the terrain when any setting is changed." )]
	public bool AutoUpdate = false;

	#endregion

	#region Chunk creation

	public void Start()
	{
		Generate();
	}

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

	#region Custom editor

	/// <summary>
	/// Auxiliar class used to allow AutoUpdate functionality and the Generate and Destroy buttons.
	/// </summary>
	[CustomEditor( typeof( LC_TerrainInstanciable ) )]
	internal class LevelScriptEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			LC_TerrainInstanciable myTarget = (LC_TerrainInstanciable)target;

			bool hasChanged = DrawDefaultInspector();

			if ( ( myTarget.AutoUpdate && hasChanged ) || GUILayout.Button( "Generate" ) )
			{
				// Update RenderMaterial
				myTarget.SetRenderMaterial();

				// Disable parallel settings
				bool parallelChunk = myTarget.ParallelChunkLoading;
				myTarget.ParallelChunkLoading = false;

				// Generate
				myTarget.Generate();

				// Restore parallel settings
				myTarget.ParallelChunkLoading = parallelChunk;
			}

			if ( GUILayout.Button( "Destroy" ) )
				myTarget.DestroyTerrain( true );
		}
	}

	#endregion
}
