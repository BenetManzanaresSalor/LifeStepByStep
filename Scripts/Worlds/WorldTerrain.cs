using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof( GenericWorld ) )]
public class WorldTerrain : LC_CubeTerrain<LC_Chunk<WorldCell>, WorldCell>
{
	#region Attributes

	#region Settings

	[SerializeField] public float WaterHeight;

	#endregion

	#region Function attributes

	protected GenericWorld World;

	#endregion

	#endregion

	#region Initialization

	public override void Generate()
	{
		if ( World == null )
			World = GetComponent<GenericWorld>();

		Player = FindObjectOfType<LC_FirstPersonController>().transform;

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

	protected override void SplitAndMergeMesh( LC_Chunk<WorldCell> chunk )
	{
		List<LC_Math.QuadTreeSector> sectors = LC_Math.SplitAndMerge(
			( x, z ) => { return chunk.Cells[x, z].RealHeight; },
			( x, y ) => { return x == y; },
			ChunkSize, true );

		foreach ( LC_Math.QuadTreeSector sector in sectors )
			CreateElementMesh( sector.Initial, sector.Final, chunk );
	}

	protected override Vector2Int GetTexPos( WorldCell cell, LC_Chunk<WorldCell> chunk )
	{
		float value;
		Vector2Int texPos = Vector2Int.zero;

		if ( cell.IsWater )
		{
			texPos.y = 0;
			value = Mathf.InverseLerp( 0, WaterHeight, cell.RealHeight );
		}
		else
		{
			texPos.y = 1;
			value = Mathf.InverseLerp( WaterHeight, MaxHeight, cell.Height );
		}
		texPos.x = Mathf.RoundToInt( value * ( TextureColumnsAndRows.x - 1 ) );

		return texPos;
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

				if ( cell.IsFree() )
				{
					WorldObject worldObj = World.GetWorldObject( cell );
					if ( worldObj != null )
					{
						worldObj = Instantiate( worldObj, chunk.Obj.transform );
						cell.TrySetContent( worldObj );
						worldObj.SetWorld( World );
						worldObj.CurrentCell = cell;
						World.WorldObjectInstanciated( worldObj );
					}
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
		bool canMove = IsPosAccesible( cellPos );
		if ( canMove )
		{
			WorldCell cell = GetCell( cellPos );
			cell.TrySetContent( worldObj );
			worldObj.CurrentCell = cell;
		}

		return canMove;
	}

	#endregion
}
