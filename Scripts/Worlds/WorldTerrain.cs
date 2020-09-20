using UnityEngine;

[RequireComponent( typeof( GenericWorld ) )]
public class WorldTerrain : LC_CubeTerrain<LC_Chunk<WorldCell>, WorldCell>
{
	#region Attributes

	#region Function attributes

	protected GenericWorld World;

	#endregion

	#endregion

	#region Initialization

	public override void Generate()
	{
		if ( World == null )
			World = GetComponent<GenericWorld>();

		Player = FindObjectOfType<FirstPersonController>().transform;

		base.Generate();
	}

	#endregion

	#region Chunk creation

	protected override LC_Chunk<WorldCell> CreateChunkInstance( Vector2Int chunkPos )
	{
		return new LC_Chunk<WorldCell>( chunkPos, ChunkSize );
	}

	protected override WorldCell CreateCell( int chunkX, int chunkZ, LC_Chunk<WorldCell> chunk )
	{
		return World.CreateCell( chunkX, chunkZ, chunk );
	}

	protected override void BuildChunk( LC_Chunk<WorldCell> chunk )
	{
		base.BuildChunk( chunk );
		InstanceWorldObjects( chunk );
	}

	protected virtual void InstanceWorldObjects( LC_Chunk<WorldCell> chunk )
	{
		for ( int x = 0; x < ChunkSize; x++ )
			for ( int y = 0; y < ChunkSize; y++ )
			{
				WorldCell cell = chunk.Cells[x, y];

				WorldObject worldObj = World.CreateWorldObject( cell );
				if ( worldObj != null )
				{
					worldObj = Instantiate( worldObj, chunk.Obj.transform );
					cell.TrySetContent( worldObj );
					worldObj.SetWorld( World );
					worldObj.CurrentCell = cell;
				}
			}
	}

	#endregion

	#region External use

	public bool IsPosAccesible( Vector2Int pos )
	{
		WorldCell cell = GetCell( pos );
		return cell != null && cell.IsFree();
	}

	public WorldObject GetCellContent( Vector2Int pos )
	{
		WorldObject worldObj = null;

		WorldCell cell = GetCell( pos );
		if ( cell != null )
			worldObj = cell.Content;

		return worldObj;
	}

	public bool TryMoveToCell( WorldObject worldObj, Vector2Int cellPos )
	{
		WorldCell cell = GetCell( cellPos );
		bool canMove = cell != null && cell.TrySetContent( worldObj );

		if ( canMove )
			worldObj.CurrentCell = cell;

		return canMove;
	}

	#endregion
}
