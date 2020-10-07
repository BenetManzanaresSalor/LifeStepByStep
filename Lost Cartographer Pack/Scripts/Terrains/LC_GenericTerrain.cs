using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Generic class parent of any terrain of Lost Cartographer Pack.
/// </summary>
public abstract class LC_GenericTerrain<Chunk, Cell> : MonoBehaviour where Chunk : LC_Chunk<Cell> where Cell : LC_Cell
{
	#region Attributes

	#region Settings	

	[Header( "Global settings" )]
	[SerializeField]
	[Tooltip( "The player used as reference position for generate the terrain.\nAlways required, including if DynamicChunkLoading is disabled." )]
	protected Transform Player;
	[SerializeField]
	[Tooltip( "Scale of each cell of the terrain." )]
	protected Vector3 CellSize = Vector3.one;
	[SerializeField]
	[Tooltip( "Two raised to this number is the number of cells per side of each chunk.\n" +
		"If ParallelChunkLoading is used lower values are recomended.\n" +
		"Big values can cause error, because the chunk mesh can exceed the maximum number of vertices (65536)." )]
	[Range( 1, 8 )]
	protected int ChunkSizeLevel = 4;
	[SerializeField]
	[Tooltip( "Distance from the player to a chunk required to load it, defined as number of chunks.\nAlong with ChunkSizeLevel, defines the final render distance." )]
	[Range( 0, 64 )]
	protected int ChunkRenderDistance = 8;
	[SerializeField]
	[Tooltip( "If the chunks have MeshCollider.\nIt can affect significantly to the performance." )]
	protected bool HasCollider = true;
	[SerializeField]
	[Tooltip( "If the chunks are loaded around the player when he moves." )]
	protected bool DynamicChunkLoading = true;
	[SerializeField]
	[Tooltip( "If use parallel Tasks to speed up chunk loading." )]
	protected bool ParallelChunkLoading = true;
	[SerializeField]
	[Tooltip( "Material to use at MeshRenderer." )]
	protected Material RenderMaterial;
	[SerializeField]
	[Tooltip( "Maximum seconds for every Update call.\n" +
		"This value is checked between every chunk load or build, avoiding further loads during that frame if the maximum time is exceeded.\n" +
		"Lower values means better framerate but slower chunk loading." )]
	[Min( float.MinValue )]
	protected float MaxUpdateTime = 1f / ( 60f * 2f );

	#endregion

	#region Function attributes

	public bool IsGenerated { get; protected set; }
	public int ChunkSize { get; protected set; }
	public Vector3 CurrentRealPos { get; protected set; }    // Equivalent to transform.position. Required for parallel chunk mesh loading.
	public Vector2Int PlayerChunkPos { get; protected set; }
	public Vector3 HalfChunk { get; protected set; }
	public float ChunkRenderRealDistance { get; protected set; }
	public Dictionary<Vector2Int, Chunk> CurrentChunks { get; protected set; }
	public Dictionary<Vector2Int, Chunk> ChunksLoading { get; protected set; }
	public Dictionary<Vector2Int, Chunk> ChunksLoaded { get; protected set; }
	public Dictionary<Vector2Int, Chunk> ChunksForMap { get; protected set; }
	public Dictionary<Vector2Int, Chunk> ChunksLoadingForMap { get; protected set; }

	public float UpdateIniTime { get; protected set; }

	protected object ChunksLoadingLock = new object();

	#endregion

	#endregion

	#region Initialization

	/// <summary>
	/// Initialize the variables, destroy the previous terrain (if exists) and generates a new terrain.
	/// </summary>
	public virtual void Generate()
	{
		ChunkSize = (int)Mathf.Pow( 2, ChunkSizeLevel );
		CurrentRealPos = transform.position;

		HalfChunk = new Vector3( CellSize.x, 0, CellSize.z ) * ( ChunkSize / 2 );
		ChunksLoading = new Dictionary<Vector2Int, Chunk>();
		ChunksLoaded = new Dictionary<Vector2Int, Chunk>();
		CurrentChunks = new Dictionary<Vector2Int, Chunk>();

		ChunksLoadingForMap = new Dictionary<Vector2Int, Chunk>();
		ChunksForMap = new Dictionary<Vector2Int, Chunk>();

		PlayerChunkPos = RealPosToChunk( Player.position );

		DestroyTerrain();
		IniTerrain();
	}

	/// <summary>
	/// Destroys all the chunks of the terrain, including all GameObjects and ParallelTasks.
	/// </summary>
	public virtual void DestroyTerrain()
	{
		IsGenerated = false;

		foreach ( Transform child in transform )
			Destroy( child.gameObject );

		if ( CurrentChunks != null )
		{
			foreach ( KeyValuePair<Vector2Int, Chunk> entry in CurrentChunks )
				entry.Value.Destroy();

			CurrentChunks.Clear();
		}

		if ( ChunksForMap != null )
		{
			foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksForMap )
				entry.Value.Destroy();

			ChunksForMap.Clear();
		}

		lock ( ChunksLoadingLock )
		{
			if ( ChunksLoading != null )
			{
				foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksLoading )
					entry.Value.Destroy();

				ChunksLoading.Clear();
			}

			if ( ChunksLoaded != null )
			{
				foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksLoaded )
					entry.Value.Destroy();

				ChunksLoaded.Clear();
			}

			if ( ChunksLoadingForMap != null )
			{
				foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksLoadingForMap )
					entry.Value.Destroy();

				ChunksLoadingForMap.Clear();
			}
		}
	}

	/// <summary>
	/// <para>Generates a new terrain around the player with square shape.</para>
	/// <para>If DynamicChunkLoading is enabled the square shape is substituted using the ChunkRenderDistance.</para>
	/// <para>If ParallelChunkLoading is enabled the chunks around the player need to be build at Update method.</para>
	/// </summary>
	protected virtual void IniTerrain()
	{
		// Always load the current player chunk
		LoadChunk( PlayerChunkPos, true );

		// Load the other chunks
		foreach ( Vector2Int chunkPos in LC_Math.AroundPositions( PlayerChunkPos, ChunkRenderDistance ) )
			LoadChunk( chunkPos );

		IsGenerated = true;
	}

	#endregion

	#region Chunk creation

	/// <summary>
	/// Loads or starts the loading of a chunk.
	/// </summary>
	/// <param name="chunkPos">Position of the chunk to load, as position at the chunk space (not terrain position).</param>
	/// <param name="ignoreParallel">Force a non-parallel chunk loading, ignoring ParallelChunkLoading.</param>
	/// <param name="isForMap">If the chunk is only required for terrain mapping, only creating the chunk cells.</param>
	protected virtual void LoadChunk( Vector2Int chunkPos, bool ignoreParallel = false, bool isForMap = false )
	{
		Chunk chunk = CreateChunkInstance( chunkPos );
		bool isParallel = ParallelChunkLoading && !ignoreParallel;

		if ( isForMap )
			ChunksLoadingForMap.Add( chunkPos, chunk );
		else if ( isParallel )
			ChunksLoading.Add( chunkPos, chunk );

		if ( isParallel )
			chunk.ParallelTask = Task.Run( () => { LoadChunkMethod( chunk, isParallel, isForMap ); } );
		else
			LoadChunkMethod( chunk, isParallel, isForMap );
	}

	/// <summary>
	/// Abstract method that instances a Chunk, from the class LC_Chunk or heiress.
	/// </summary>
	/// <param name="chunkPos">Position of the chunk to load, as position at the chunk space (not terrain position).</param>
	/// <returns></returns>
	protected abstract Chunk CreateChunkInstance( Vector2Int chunkPos );

	/// <summary>
	/// <para>Specific method for load a chunk, once the chunk is instanced.</para>
	/// <para>Used at LoadChunk for parallel or non-parallel loading.</para>
	/// <para>IMPORTANT: If is a parallel execution this method can't instanciate any child of GameObject.</para>
	/// </summary>
	/// <param name="chunk">Chunk instance.</param>
	/// <param name="isParallel">If the chunk is loading in parallel.</param>
	/// <param name="isForMap">If the chunk is loading for the terrain mapping.</param>
	protected virtual void LoadChunkMethod( Chunk chunk, bool isParallel, bool isForMap )
	{
		CreateChunkCells( chunk );

		if ( !isForMap )
		{
			ComputeMesh( chunk );
			chunk.BuildMesh();
		}

		ChunkLoaded( chunk, isParallel, isForMap );
	}

	/// <summary>
	/// <para>Abstract mehtod that creates the cells of a chunk. A assingnation of the chunk.Cells variable is required.</para>
	/// <para>IMPORTANT: If is a parallel execution, this method can't instanciate any child of GameObject.</para>
	/// </summary>
	/// <param name="chunk">Chunk to assign the cells.</param>
	/// <returns></returns>
	protected abstract void CreateChunkCells( Chunk chunk );

	/// <summary>
	/// <para>Notify the chunk loading. If is a parallel execution, is added to the ChunksLoaded list in order to be build at BuildParallelyLoadedChunks method.</para>
	/// <para>If the chunk is for map, it is added to ChunksForMap list.</para>
	/// <para>Otherwise, the chunk is build.</para>
	/// <para>IMPORTANT: If is a parallel execution, this method can't instanciate any child of GameObject.</para>
	/// </summary>
	/// <param name="chunk">Chunk instance.</param>
	/// <param name="isParellel">If the chunk is loading in parallel.</param>
	/// <param name="isForMap">If the chunk is loading for the terrain mapping.</param>
	protected virtual void ChunkLoaded( Chunk chunk, bool isParallel, bool isForMap )
	{
		if ( isParallel )
			Monitor.Enter( ChunksLoadingLock );

		if ( isForMap )
		{
			ChunksLoadingForMap.Remove( chunk.Position );
			ChunksForMap.Add( chunk.Position, chunk );
		}
		else if ( isParallel )
		{
			ChunksLoading.Remove( chunk.Position );
			ChunksLoaded.Add( chunk.Position, chunk );
		}

		if ( isParallel )
			Monitor.Exit( ChunksLoadingLock );
		else if ( !isForMap )
		{
			BuildChunk( chunk );
			CurrentChunks.Add( chunk.Position, chunk );
		}
	}

	#region Mesh

	/// <summary>
	/// <para>Abstract method that computes the mesh data (vertices, triangles, UVs and, alternatively, the normals) for a chunk.</para>
	/// <para>All this data has to be assigned to the corresponding variables of the chunk.</para>
	/// </summary>
	/// <param name="chunk">Chunk to assign the mesh data.</param>
	protected abstract void ComputeMesh( Chunk chunk );

	/// <summary>
	/// <para>Create the GameObject instance of a chunk and add the components.</para>
	/// <para>The components are MeshFilter(with the Mesh), MeshRenderer and, if HasCollider is enabled, MeshCollider.</para>
	/// </summary>
	/// <param name="chunk">Chunk to build.</param>
	protected virtual void BuildChunk( Chunk chunk )
	{
		chunk.Obj = new GameObject();
		chunk.Obj.transform.parent = this.transform;
		chunk.Obj.name = "Chunk_" + chunk.Position;

		Mesh mesh = new Mesh
		{
			vertices = chunk.VerticesArray,
			triangles = chunk.TrianglesArray,
			uv = chunk.UVsArray
		};
		mesh.RecalculateBounds();

		if ( chunk.Normals != null )
			mesh.normals = chunk.Normals;
		else
			mesh.RecalculateNormals();

		mesh.Optimize();

		MeshFilter renderMeshFilter = chunk.Obj.AddComponent<MeshFilter>();
		renderMeshFilter.mesh = mesh;

		chunk.Obj.AddComponent<MeshRenderer>().material = RenderMaterial;

		if ( HasCollider )
		{
			MeshCollider renderMeshCollider = chunk.Obj.AddComponent<MeshCollider>();
			renderMeshCollider.sharedMesh = mesh;
		}
	}

	#endregion

	#endregion

	#region Update

	/// <summary>
	/// Updates the terrain if is required.
	/// </summary>
	protected virtual void Update()
	{
		if ( IsGenerated )
		{
			UpdateIniTime = Time.realtimeSinceStartup;

			// Update useful variables
			CurrentRealPos = transform.position;    // Required for parallel functions and others
			PlayerChunkPos = RealPosToChunk( Player.position ); // Required for DynamicChunkLoading
			ChunkRenderRealDistance = ChunkRenderDistance * ChunkSize * Mathf.Max( CellSize.x, CellSize.z );    // Required for DynamicChunkLoading

			// Check chunks loaded parallelly (if remains time)
			if ( ParallelChunkLoading && InMaxUpdateTime() )
				BuildParallelyLoadedChunks();

			if ( DynamicChunkLoading )
			{
				// Load the current player chunk if isn't loaded
				CheckPlayerCurrentChunk();

				// Update chunks required (if remains time)
				if ( InMaxUpdateTime() )
					DynamicChunksUpdate();
			}
		}
	}

	/// <summary>
	/// <para>Checks the player current chunk, that always has to be build.</para>
	/// <para>If is loading, waits for it to load and then build it.</para>
	/// <para>If it isn't loaded, loads it.</para>
	/// </summary>
	protected virtual void CheckPlayerCurrentChunk()
	{
		Monitor.Enter( ChunksLoadingLock );

		if ( !CurrentChunks.ContainsKey( PlayerChunkPos ) && !ChunksLoaded.ContainsKey( PlayerChunkPos ) )
		{
			bool isLoading = ChunksLoading.TryGetValue( PlayerChunkPos, out Chunk playerChunk );
			Monitor.Exit( ChunksLoadingLock );
			if ( isLoading )
			{
				playerChunk.ParallelTask.Wait();  // Wait parallel loading to end
				BuildParallelyLoadedChunks();    // Built the chunk (and the others if are required)
			}
			else
			{
				LoadChunk( PlayerChunkPos, true );    // Ignore parallel because is impossible continue playing without this chunk
			}
		}
		else
			Monitor.Exit( ChunksLoadingLock );
	}

	/// <summary>
	/// Checks if the time since the start of the Update method is greater than the MaxUpdateTime.
	/// </summary>
	/// <returns></returns>
	protected virtual bool InMaxUpdateTime()
	{
		return ( Time.realtimeSinceStartup - UpdateIniTime ) <= MaxUpdateTime;
	}

	/// <summary>
	/// Checks if a new iteration of a loop will be in the MaxUpdateTime using the average iteration time.
	/// </summary>
	/// <param name="averageIterationTime">Average time of the loop iteration.</param>
	/// <returns></returns>
	protected virtual bool InMaxUpdateTime( float averageIterationTime )
	{
		return ( Time.realtimeSinceStartup - UpdateIniTime + averageIterationTime ) <= MaxUpdateTime;
	}

	/// <summary>
	/// Builds the chunks loaded parallely. For each chunk it uses the InMaxUpdateTime method, breaking the loop if the MaxUpdateTime is exceeded.
	/// </summary>
	protected virtual void BuildParallelyLoadedChunks()
	{
		if ( ChunksLoaded.Count > 0 )
		{
			lock ( ChunksLoadingLock )
			{
				Chunk chunk;
				List<Vector2Int> chunksBuilt = new List<Vector2Int>( ChunksLoaded.Count );
				float loopStartTime = Time.realtimeSinceStartup;
				float numIterations = 0;
				float averageIterationTime = 0;
				foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksLoaded )
				{
					if ( InMaxUpdateTime( averageIterationTime ) )
					{
						chunk = entry.Value;
						if ( IsChunkRequired( chunk.Position ) )
						{
							BuildChunk( chunk );
							CurrentChunks.Add( chunk.Position, chunk );
						}
						else
						{
							chunk.Destroy();
						}

						chunksBuilt.Add( entry.Key );

						numIterations++;
						averageIterationTime = ( Time.realtimeSinceStartup - loopStartTime ) / numIterations;
					}
					else
						break;
				}

				// Delete chunks already built
				foreach ( Vector2Int key in chunksBuilt )
					ChunksLoaded.Remove( key );
			}
		}
	}

	/// <summary>
	/// <para>Manages the chunks required and the current chunks using the player position.</para>
	/// <para>If a chunk is required and not loaded, it loads it (or starts the load if ParallelChunkLoading is enabled).</para>
	/// <para>If a chunk is not required, destroy it.</para>
	/// <para>Moreover, when is loading the chunks required, it uses the InMaxUpdateTime method, breaking the loop if the MaxUpdateTime is exceeded.</para>
	/// </summary>
	protected virtual void DynamicChunksUpdate()
	{
		Dictionary<Vector2Int, object> chunkRequired = DynamicChunksRequired(); // Use a dictionary for faster searchs

		// Check chunks already created
		List<Vector2Int> chunksToDestroy = new List<Vector2Int>();
		foreach ( KeyValuePair<Vector2Int, Chunk> entry in CurrentChunks )
		{
			// If already created, don't reload
			if ( chunkRequired.ContainsKey( entry.Key ) )
			{
				chunkRequired.Remove( entry.Key );
			}
			// If don't required, unload
			else
			{
				chunksToDestroy.Add( entry.Key );
				entry.Value.Destroy();
			}
		}

		// Remove chunks don't required
		foreach ( Vector2Int chunkPos in chunksToDestroy )
			CurrentChunks.Remove( chunkPos );

		if ( InMaxUpdateTime() )
		{
			lock ( ChunksLoadingLock )
			{
				// Ignore chunks that are loading
				foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksLoading )
					if ( chunkRequired.ContainsKey( entry.Key ) )
						chunkRequired.Remove( entry.Key );

				// Ignore chunks that are already loaded
				foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksLoaded )
					if ( chunkRequired.ContainsKey( entry.Key ) )
						chunkRequired.Remove( entry.Key );

				// Load the other chunks
				float loopStartTime = Time.realtimeSinceStartup;
				float numIterations = 0;
				float averageIterationTime = 0;
				foreach ( KeyValuePair<Vector2Int, object> entry in chunkRequired )
				{
					if ( InMaxUpdateTime( averageIterationTime ) )
					{
						LoadChunk( entry.Key );

						numIterations++;
						averageIterationTime = ( Time.realtimeSinceStartup - loopStartTime ) / numIterations;
					}
					else
						break;
				}
			}
		}
	}

	/// <summary>
	/// <para>Calculate the chunks required dynamically using the player position and the ChunkRenderDistance.</para>
	/// <para>Returns a dictionary instead of a list for performance reasons.</para>
	/// </summary>
	/// <returns></returns>
	protected virtual Dictionary<Vector2Int, object> DynamicChunksRequired()
	{
		Dictionary<Vector2Int, object> chunksRequired = new Dictionary<Vector2Int, object>();

		// Always load the player current chunk		
		chunksRequired.Add( PlayerChunkPos, null );

		if ( ChunkRenderDistance > 0 )
		{
			int radius = ChunkRenderDistance + 1;

			Vector2Int topLeftCorner;
			Vector2Int chunkPos = new Vector2Int();
			int yIncrement = 1;
			for ( int currentRadius = 1; currentRadius < radius; currentRadius++ )
			{
				topLeftCorner = PlayerChunkPos + Vector2Int.one * -1 * currentRadius;

				for ( int x = 0; x <= currentRadius * 2; x++ )
				{
					yIncrement = ( x == 0 || x == currentRadius * 2 ) ? 1 : currentRadius * 2;
					for ( int y = 0; y <= currentRadius * 2; y += yIncrement )
					{
						chunkPos.x = topLeftCorner.x + x;
						chunkPos.y = topLeftCorner.y + y;

						// If isn't PlayerChunkPos (because is always loaded) and is required
						if ( chunkPos != PlayerChunkPos && IsChunkRequired( chunkPos ) )
							chunksRequired.Add( chunkPos, null );
					}
				}
			}
		}

		return chunksRequired;
	}

	#endregion

	#region Mapping

	/// <summary>
	/// Equivalent to DynamicChunksUpdate but for terrain mapping. Loads the chunks required for the map and unloads the unrequired ones.
	/// </summary>
	/// <param name="bottomLeftPos">Bottom left corner of the map.</param>
	/// <param name="topRightPos">Top right corner of the map.</param>
	/// <param name="inMaxUpdateTime">Function from the mapping class that checks if the corresponding MaxUpdateTime is exceeded.</param>
	public virtual void UpdateChunksForMap( Vector2Int bottomLeftPos, Vector2Int topRightPos, System.Func<bool> inMaxUpdateTime )
	{
		Vector2Int bottomLeftChunkPos = TerrainPosToChunk( bottomLeftPos );
		Vector2Int topRightChunkPos = TerrainPosToChunk( topRightPos );
		Vector2Int mapSize = topRightChunkPos - bottomLeftChunkPos;

		// Destroy the don't required chunks
		List<Vector2Int> chunksToDestroy = new List<Vector2Int>();
		foreach ( KeyValuePair<Vector2Int, Chunk> entry in ChunksForMap )
			if ( entry.Key.x < bottomLeftChunkPos.x || entry.Key.x > topRightChunkPos.x ||
				entry.Key.y < bottomLeftChunkPos.y || entry.Key.y > topRightChunkPos.y )
			{
				entry.Value.Destroy();
				chunksToDestroy.Add( entry.Key );
			}

		foreach ( Vector2Int pos in chunksToDestroy )
			ChunksForMap.Remove( pos );

		// Load the chunks required for map
		Vector2Int chunkPos = new Vector2Int();
		for ( int x = 0; x <= mapSize.x; x++ )
		{
			for ( int y = 0; y <= mapSize.y; y++ )
			{
				chunkPos.x = bottomLeftChunkPos.x + x;
				chunkPos.y = bottomLeftChunkPos.y + y;

				// If no loaded neither loading
				if ( !CurrentChunks.ContainsKey( chunkPos ) &&
					!ChunksLoaded.ContainsKey( chunkPos ) &&
					!ChunksLoading.ContainsKey( chunkPos ) &&
					!ChunksForMap.ContainsKey( chunkPos ) &&
					!ChunksLoadingForMap.ContainsKey( chunkPos ) )
				{
					LoadChunk( chunkPos, ParallelChunkLoading, true );
				}
			}
		}
	}

	#endregion

	#region Auxiliar

	/// <summary>
	/// Checks if a chunk is required using the player position, chunk position, and the ChunkRenderDistance.
	/// </summary>
	/// <param name="chunkPos">Position of the chunk.</param>
	/// <returns></returns>
	public virtual bool IsChunkRequired( Vector2Int chunkPos )
	{
		bool isrequired = false;

		if ( DynamicChunkLoading )
		{
			isrequired = chunkPos == PlayerChunkPos;

			// If isn't the player current chunk
			if ( !isrequired )
			{
				Vector3 chunkRealPosition = ChunkPosToReal( chunkPos );
				Vector3 offsetToPlayer = chunkRealPosition - Player.position;
				offsetToPlayer.y = 0; // Ignore height offset

				isrequired = offsetToPlayer.magnitude <= ChunkRenderRealDistance;
			}
		}
		else
		{
			Vector2Int distanceToPlayer = new Vector2Int( Mathf.Abs( PlayerChunkPos.x - chunkPos.x ), Mathf.Abs( PlayerChunkPos.y - chunkPos.y ) );
			isrequired = distanceToPlayer.x <= ChunkRenderDistance && distanceToPlayer.y <= ChunkRenderDistance;
		}

		return isrequired;
	}

	#endregion

	#region External use

	public virtual Vector3 TerrainPosToReal( int x, float height, int z )
	{
		return CurrentRealPos + new Vector3( x * CellSize.x, height * CellSize.y, z * CellSize.z ) - HalfChunk;
	}

	public virtual Vector3 TerrainPosToReal( Vector2Int terrainPos, float height )
	{
		return TerrainPosToReal( terrainPos.x, height, terrainPos.y );
	}

	public virtual Vector3 TerrainPosToReal( Cell cell )
	{
		return TerrainPosToReal( cell.TerrainPos, cell.Height );
	}

	public virtual Vector3 ChunkPosToReal( Vector2Int chunkPosition )
	{
		return CurrentRealPos + new Vector3( chunkPosition.x * ChunkSize * CellSize.x, 0, chunkPosition.y * ChunkSize * CellSize.z );
	}

	public virtual Vector3Int RealPosToTerrain( Vector3 realPos )
	{
		Vector3 relativePos = realPos - CurrentRealPos + HalfChunk;
		return new Vector3Int( (int)( relativePos.x / CellSize.x ), (int)( relativePos.y / CellSize.y ), (int)( relativePos.z / CellSize.z ) );
	}

	public virtual Vector3Int GetPlayerTerrainPos()
	{
		return RealPosToTerrain( Player.position );
	}

	public virtual Vector2Int TerrainPosToChunk( Vector2Int terrainPos )
	{
		Vector2 chunkPos = new Vector2( (float)terrainPos.x / ChunkSize, (float)terrainPos.y / ChunkSize );

		// Adjust negative postions
		float decimalX = chunkPos.x - (int)chunkPos.x;
		if ( decimalX < 0f )
			chunkPos.x -= 1;
		float decimalY = chunkPos.y - (int)chunkPos.y;
		if ( decimalY < 0f )
			chunkPos.y -= 1;

		return new Vector2Int( (int)chunkPos.x, (int)chunkPos.y );
	}

	public virtual Vector2Int TerrainPosToChunk( Vector3Int terrainPos )
	{
		return TerrainPosToChunk( new Vector2Int( terrainPos.x, terrainPos.z ) );
	}

	public virtual Vector2Int RealPosToChunk( Vector3 realPos )
	{
		Vector3Int terrainPos = RealPosToTerrain( realPos );
		return TerrainPosToChunk( terrainPos );
	}

	public virtual Chunk GetChunk( Vector2Int terrainPos )
	{
		Vector2Int chunkPos = TerrainPosToChunk( terrainPos );
		return CurrentChunks.ContainsKey( chunkPos ) ? CurrentChunks[chunkPos] : null;
	}

	public virtual Chunk GetChunk( Vector3Int terrainPos )
	{
		return GetChunk( new Vector2Int( terrainPos.x, terrainPos.z ) );
	}

	public virtual Chunk GetChunk( Vector3 realPos )
	{
		return GetChunk( RealPosToTerrain( realPos ) );
	}

	public virtual Cell GetCell( Vector2Int terrainPos, bool isForMap = false )
	{
		Cell cell = null;

		Chunk chunk = GetChunk( terrainPos );

		// Check ChunksForMap
		if ( chunk == null && isForMap )
		{
			Vector2Int chunkPos = TerrainPosToChunk( terrainPos );
			ChunksForMap.TryGetValue( chunkPos, out chunk );
		}

		// Get cell from the chunk if it exists
		if ( chunk != null )
		{
			Vector2Int posInChunk = new Vector2Int( LC_Math.Mod( terrainPos.x, ChunkSize ),
				LC_Math.Mod( terrainPos.y, ChunkSize ) );

			cell = chunk.Cells[posInChunk.x, posInChunk.y];
		}

		return cell;
	}

	public virtual Cell GetCell( Vector3 realPos, bool isForMap = false )
	{
		Vector3Int terrainPos = RealPosToTerrain( realPos );
		return GetCell( new Vector2Int( terrainPos.x, terrainPos.z ), isForMap );
	}

	#endregion
}