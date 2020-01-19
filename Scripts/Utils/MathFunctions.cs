using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class MathFunctions
{
	#region Constants

	public static readonly List<Vector2Int> FourDirections2D = new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
	public static readonly List<Vector2Int> EightDirections2D = new List<Vector2Int>
		{ Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
		Vector2Int.up + Vector2Int.right, Vector2Int.up + Vector2Int.left, Vector2Int.down + Vector2Int.right, Vector2Int.down + Vector2Int.left};

	#endregion

	#region Statistics

	public static long AverageTimeAstar { get; private set; }
	public static long MaxTimeAstar { get; private set; }
	private static int NumMeasuresTimeAstarPath = 0;

	public static float AveragePathlengthAstar { get; private set; }
	public static float MaxPathlengthAstar { get; private set; }
	private static int NumMeasuresPathlengthAstar = 0;

	public static float AverageCheckedPositionsAstar { get; private set; }
	public static float MaxCheckedPositionsAstar { get; private set; }
	private static int NumMeasuresCheckedPositionsAstar = 0;

	private static void UpdateAstarStatistics( long time, int pathLength, int checkedPositions )
	{
		AverageTimeAstar = ( AverageTimeAstar * NumMeasuresTimeAstarPath + time ) / ( NumMeasuresTimeAstarPath + 1 );
		NumMeasuresTimeAstarPath++;
		if ( time > MaxTimeAstar )
		{
			MaxTimeAstar = time;
		}

		AveragePathlengthAstar = ( AveragePathlengthAstar * NumMeasuresPathlengthAstar + pathLength ) / ( NumMeasuresPathlengthAstar + 1 );
		NumMeasuresPathlengthAstar++;
		if ( pathLength > MaxPathlengthAstar )
		{
			MaxPathlengthAstar = pathLength;
		}

		AverageCheckedPositionsAstar = ( AverageCheckedPositionsAstar * NumMeasuresCheckedPositionsAstar + checkedPositions ) / ( NumMeasuresCheckedPositionsAstar + 1 );
		NumMeasuresCheckedPositionsAstar++;
		if ( checkedPositions > MaxCheckedPositionsAstar )
		{
			MaxCheckedPositionsAstar = checkedPositions;
		}
	}

	public static void ResetStatistics()
	{
		AverageTimeAstar = 0;
		MaxTimeAstar = 0;
		NumMeasuresTimeAstarPath = 0;

		AveragePathlengthAstar = 0;
		MaxPathlengthAstar = 0;
		NumMeasuresPathlengthAstar = 0;

		AverageCheckedPositionsAstar = 0;
		MaxCheckedPositionsAstar = 0;
		NumMeasuresCheckedPositionsAstar = 0;
	}

	public static string GetStatistics()
	{
		return $"Astar statistics:" +
					$" Average time = {AverageTimeAstar} ms" +
					$" Max time = {MaxTimeAstar} ms" +
					$" Average path length = {AveragePathlengthAstar}" +
					$" Max path length = {MaxPathlengthAstar}" +
					$" Average checked positions = {AverageCheckedPositionsAstar}" +
					$" Max checked positions = {MaxCheckedPositionsAstar}" +
					$" Relation path length <-> checked positions = {( AveragePathlengthAstar / AverageCheckedPositionsAstar ).ToString( "f3" )}";
	}

	#endregion

	#region Pathfinding

	public delegate bool IsPositionAccesible( Vector2Int position );

	public static List<Vector2Int> PathfindingWithReusing( Vector2Int origin, Vector2Int target, IsPositionAccesible isPositionAccesible, List<Vector2Int> pathToLastTarget, Vector2Int lastTarget, int maxPathLenght )
	{
		// If any calculated path
		if ( pathToLastTarget == null || pathToLastTarget.Count == 0 )
		{
			pathToLastTarget = Pathfinding( origin, target, isPositionAccesible, maxPathLenght );
		}
		// Is some path previously calculated
		else
		{
			int positionCount;
			bool isPathClear = true;
			bool pathContainsTarget = false;
			Vector2Int currentposition;

			// Check is the path is clear and if some position is already done
			for ( positionCount = 0; positionCount < pathToLastTarget.Count && isPathClear && !pathContainsTarget; positionCount++ )
			{
				currentposition = pathToLastTarget[positionCount];

				// Check if the path has a not accesible position
				isPathClear &= isPositionAccesible( currentposition );

				pathContainsTarget = pathToLastTarget[positionCount].Equals( target );
			}

			// If exists path to a not changed target
			if ( lastTarget.Equals( target ) )
			{
				if ( !isPathClear )
				{
					pathToLastTarget = Pathfinding( origin, target, isPositionAccesible, maxPathLenght );
				}
			}
			// If new target
			else
			{
				// If is impossible to reuse path calculation
				if ( !pathContainsTarget || !isPathClear )
				{
					pathToLastTarget = Pathfinding( origin, target, isPositionAccesible, maxPathLenght );
				}
			}
		}

		return pathToLastTarget;
	}

	public static List<Vector2Int> Pathfinding( Vector2Int origin, Vector2Int target, IsPositionAccesible isPositionAccesible, int maxPathLenght )
	{
		List<Vector2Int> path;
		bool directPathArrives;

		// First, try direct path
		path = DirectPath( origin, target, isPositionAccesible );
		directPathArrives = path.Count != 0 && IsTouchingTarget( path[path.Count - 1], target, isPositionAccesible );

		// If a direct path not arrives to target
		if ( !directPathArrives )
		{
			path = AstarPath( origin, target, isPositionAccesible, maxPathLenght );
		}

		return path;
	}

	public class PositionInPath
	{
		public Vector2Int Position { get; }
		public float AcummulatedCost { get; }
		public PositionInPath OriginPosition { get; }

		public PositionInPath( Vector2Int position, float acummulatedCost, PositionInPath originPosition )
		{
			Position = position;
			AcummulatedCost = acummulatedCost;
			OriginPosition = originPosition;
		}

		public override bool Equals( object obj )
		{
			return obj is PositionInPath && ( (PositionInPath)obj ).Position.Equals( Position );
		}

		public override int GetHashCode()
		{
			return Position.GetHashCode();
		}

		public override string ToString()
		{
			return Position.ToString();
		}
	}

	// TODO : Best control of blocked targets
	public static List<Vector2Int> AstarPath( Vector2Int origin, Vector2Int target, IsPositionAccesible isPositionAccesible, int maxCheckedPositions )
	{
		Stopwatch chrono = new Stopwatch();
		chrono.Start();

		List<Vector2Int> path = new List<Vector2Int>();
		List<PositionInPath> checkedPositions = new List<PositionInPath>();
		List<PositionInPath> remainingPositions = new List<PositionInPath>() { new PositionInPath( origin, 0, null ) };

		PositionInPath currentPositionInPath = null;
		PositionInPath closestToTarget = remainingPositions[0];
		float closestDistanceToTarget = closestToTarget.Position.Distance( target );
		PositionInPath nextPositionInPath;
		float auxDistance;

		bool destinyNotAccesible = false;
		bool targetReached = false;

		// Calcule path 
		while ( !targetReached && !destinyNotAccesible )
		{
			// Select next position
			currentPositionInPath = remainingPositions[0];
			foreach ( PositionInPath positionInPath in remainingPositions )
			{
				if ( ( positionInPath.Position.Distance( target ) + positionInPath.AcummulatedCost ) <
					( currentPositionInPath.Position.Distance( target ) + currentPositionInPath.AcummulatedCost ) )
				{
					currentPositionInPath = positionInPath;
				}
			}

			// Remove from remaining and add to checked
			remainingPositions.Remove( currentPositionInPath );
			checkedPositions.Add( currentPositionInPath );

			// Check around positions
			foreach ( Vector2Int movement in EightDirections2D )
			{
				nextPositionInPath = new PositionInPath( currentPositionInPath.Position + movement,
					currentPositionInPath.AcummulatedCost + movement.magnitude,
					currentPositionInPath );

				// If the position isn't checked and is accesible
				if ( !checkedPositions.Contains( nextPositionInPath ) && IsPossibleDirection( currentPositionInPath.Position, movement, isPositionAccesible ) )
				{
					remainingPositions.Add( nextPositionInPath );

					// Check closest to target
					auxDistance = nextPositionInPath.Position.Distance( target );
					if ( auxDistance < closestDistanceToTarget )
					{
						closestToTarget = nextPositionInPath;
						closestDistanceToTarget = auxDistance;
					}
				}
			}

			// Check end of path
			if ( IsTouchingTarget( currentPositionInPath.Position, target, isPositionAccesible ) )
			{
				targetReached = true;
			}
			else
			{
				destinyNotAccesible = remainingPositions.Count == 0 || checkedPositions.Count > maxCheckedPositions;
			}
		}

		// If is impossible to acces to the target use the closest position as desitiny
		if ( destinyNotAccesible )
		{
			currentPositionInPath = closestToTarget;
		}

		// If the currentPosition ( best last position found ) is closer to target than the origin is a good path
		if ( currentPositionInPath.Position.Distance( target ) < origin.Distance( target ) )
		{
			// Do the reverse path, from the end to the origin position
			while ( currentPositionInPath.Position != origin )
			{
				path.Add( currentPositionInPath.Position );
				currentPositionInPath = currentPositionInPath.OriginPosition;
			}
			path.Reverse();

			UpdateAstarStatistics( chrono.ElapsedMilliseconds, path.Count, checkedPositions.Count );
		}

		chrono.Stop();

		return path;
	}

	public static List<Vector2Int> DirectPath( Vector2Int origin, Vector2Int target, IsPositionAccesible isPositionAccesible )
	{
		List<Vector2Int> path = new List<Vector2Int>();

		if ( !IsTargetAccesBlocked( target, isPositionAccesible ) )
		{
			Vector2Int position = origin;
			Vector2Int movement = new Vector2Int();

			bool targetNotAccesible = false;

			while ( !position.Equals( target ) && !targetNotAccesible )
			{
				movement.x = -Clamp( position.x.CompareTo( target.x ), -1, 1 );
				movement.y = -Clamp( position.y.CompareTo( target.y ), -1, 1 );

				if ( IsPossibleDirection( position, movement, isPositionAccesible ) )
				{
					position = position + movement;
					path.Add( position );
				}
				else
				{
					targetNotAccesible = true;
				}
			}
		}

		return path;
	}

	public static bool IsTargetAccesBlocked( Vector2Int target, IsPositionAccesible isPositionAccesible )
	{
		bool isAccesible = false;

		// For all directions, while a acces to the target isn't found
		for ( int i = 0; i < EightDirections2D.Count && !isAccesible; i++ )
		{
			isAccesible = isPositionAccesible( target + EightDirections2D[i] );
		}

		return !isAccesible;
	}

	#endregion

	#region Random generation

	public static float[,] PerlinNoiseMap( int columns, int rows, float seed, float octaves, float persistance, float lacunarity, float minValue, float maxValue )
	{
		float[,] map = new float[columns, rows];

		// Initializate map
		float currentMin = float.MaxValue;
		float currentMax = float.MinValue;
		float perlinValue = 0;
		float amplitude;
		float frequency;
		for ( int col = 0; col < map.GetLength( 0 ); col++ )
		{
			for ( int row = 0; row < map.GetLength( 1 ); row++ )
			{
				perlinValue = 0;
				amplitude = 1;
				frequency = 1;

				for ( int oct = 0; oct < octaves; oct++ )
				{
					perlinValue += Mathf.PerlinNoise( seed + col * frequency, seed + row * frequency ) * 2 - 1; // * 2 + 1 For positive and negative jumps

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				currentMin = Min( perlinValue, currentMin );
				currentMax = Max( perlinValue, currentMax );

				map[col, row] = perlinValue;
			}
		}

		// Normalize map values in min-max range
		for ( int x = 0; x < map.GetLength( 0 ); x++ )
		{
			for ( int z = 0; z < map.GetLength( 1 ); z++ )
			{
				map[x, z] = minValue + Mathf.InverseLerp( currentMin, currentMax, map[x, z] ) * ( maxValue - minValue );
			}
		}

		return map;
	}

	public static Vector2Int PseudorandomDirection( Vector2Int origin, Vector2Int lastDirection, System.Random randGenerator, int sameDirectionProbability, IsPositionAccesible isPositionAccesible )
	{
		Vector2Int nextDirection = Vector2Int.zero;
		lastDirection.TransformToDirection();

		int sameDirectionProb = Clamp( sameDirectionProbability, 0, 100 );
		bool useSameDirection = randGenerator.Next( 0, 100 ) <= sameDirectionProb;

		// If is possible continue in the same direction ( not 0,0 )
		if ( !lastDirection.Equals( Vector2Int.zero ) && useSameDirection && IsPossibleDirection( origin, lastDirection, isPositionAccesible ) )
		{
			nextDirection = lastDirection;
		}
		// Search a new random direction different of last
		else
		{
			List<Vector2Int> directions = new List<Vector2Int>( EightDirections2D );
			directions.Remove( lastDirection );
			directions.Remove( lastDirection * -1 ); // By now, discard the opposite of last direction for avoid loops

			Vector2Int possibleDirection;
			while ( directions.Count > 0 && nextDirection.Equals( Vector2Int.zero ) )
			{
				possibleDirection = directions[randGenerator.Next( 0, directions.Count )];

				if ( IsPossibleDirection( origin, possibleDirection, isPositionAccesible ) )
				{
					nextDirection = possibleDirection;
				}
				else
				{
					directions.Remove( possibleDirection );
				}
			}

			// If any other direction is possible, check the opposite to last direction
			possibleDirection = lastDirection * -1;
			if ( nextDirection.Equals( Vector2Int.zero ) && IsPossibleDirection( origin, possibleDirection, isPositionAccesible ) )
			{
				nextDirection = possibleDirection;
			}
		}

		return nextDirection;
	}

	#endregion

	#region Up scale matrix	

	public static T[,] UpScaleMatrix<T>( Func<int, int, T> get, Vector2Int originalDim, Vector2Int resultDim, int upScale, Func<T, float, T> multiplyFunc, Func<T, T, T> sumFunc )
	{
		T[,] result = new T[resultDim.x, resultDim.y];

		for ( int i = 0; i < resultDim.x; i++ )
		{
			for ( int j = 0; j < resultDim.y; j++ )
			{
				result[i, j] = UpScaleMatrixValue( get, upScale, i, j, originalDim, multiplyFunc, sumFunc );
			}
		}

		return result;
	}

	public static T UpScaleMatrixValue<T>( Func<int, int, T> get, int upScale, int col, int row, Vector2Int originalDim, Func<T, float, T> multiplyFunc, Func<T, T, T> sumFunc )
	{
		float originalCol = Clamp( col / (float)upScale, 0, originalDim.x - 1 );
		float originalRow = Clamp( row / (float)upScale, 0, originalDim.y - 1 );
		return InterpolateValueInMatrix( get, originalCol, originalRow, multiplyFunc, sumFunc );
	}

	public static T InterpolateValueInMatrix<T>( Func<int, int, T> get, float col, float row, Func<T, float, T> multiplyFunc, Func<T, T, T> sumFunc )
	{
		T result;

		int matrixIntCol = (int)col;
		float matrixDecimalCol = col - matrixIntCol;

		int matrixIntRow = (int)row;
		float matrixDecimalRow = row - matrixIntRow;

		result = multiplyFunc( multiplyFunc( get( matrixIntCol, matrixIntRow ), 1f - matrixDecimalCol ), 1f - matrixDecimalRow );
		if ( matrixDecimalCol > 0 )
			result = sumFunc( result, multiplyFunc( multiplyFunc( get( matrixIntCol + 1, matrixIntRow ), matrixDecimalCol ), 1f - matrixDecimalRow ) );
		if ( matrixDecimalRow > 0 )
			result = sumFunc( result, multiplyFunc( multiplyFunc( get( matrixIntCol, matrixIntRow + 1 ), 1f - matrixDecimalCol ), matrixDecimalRow ) );
		if ( matrixDecimalCol > 0 && matrixDecimalRow > 0 )
			result = sumFunc( result, multiplyFunc( multiplyFunc( get( matrixIntCol + 1, matrixIntRow + 1 ), matrixDecimalCol ), matrixDecimalRow ) );

		return result;
	}

	public static T InterpolateValueInArray<T>( Func<int, T> get, float decimalIndex, Func<T, float, T> multiplyFunc, Func<T, T, T> sumFunc )
	{
		T result;

		int arrayIntPos = (int)decimalIndex;
		float arrayDecimalPos = decimalIndex % 1f;

		result = multiplyFunc( get( arrayIntPos ), 1f - arrayDecimalPos );
		if ( arrayDecimalPos > 0f ) result = sumFunc( result, multiplyFunc( get( arrayIntPos + 1 ), arrayDecimalPos ) );

		return result;
	}

	#endregion

	#region Spatial decomposition

	public struct QuadTreeSector
	{
		public Vector2Int Initial;
		public Vector2Int Final;
		public Vector2Int Size { get { return Final - Initial + Vector2Int.one; } }

		public QuadTreeSector( Vector2Int initial, Vector2Int final )
		{
			Initial = initial;
			Final = final;
		}

		public QuadTreeSector( int x1, int y1, int x2, int y2 )
		{
			Initial = new Vector2Int( x1, y1 );
			Final = new Vector2Int( x2, y2 );
		}

		public override string ToString()
		{
			return Initial + " " + Final;
		}

		public static bool IsMergeableNotInvertible<T>( QuadTreeSector original, QuadTreeSector other, Func<int, int, T> get, Func<T, T, bool> equals )
		{
			// TODO : Check if is really mergeable when contained
			bool notContained = original.Initial != other.Initial && original.Final != other.Final && original.Initial != other.Final && original.Final != other.Initial;
			bool horitzontalMatch = original.Final.x == other.Initial.x - 1 && original.Final.y == other.Final.y && original.Initial.y == other.Initial.y;
			bool verticalMatch = original.Final.y == other.Final.y - 1 && original.Final.x == other.Final.x && original.Initial.x == other.Initial.x;
			bool equalsValues = equals( get( other.Initial.x, other.Initial.y ), get( original.Initial.x, original.Initial.y ) );
			return notContained && ( horitzontalMatch || verticalMatch ) && equalsValues;
		}

		public QuadTreeSector TryMerge<T>( QuadTreeSector other, Func<int, int, T> get, Func<T, T, bool> equals )
		{
			QuadTreeSector result;

			if ( IsMergeableNotInvertible( this, other, get, equals ) )
			{
				result = new QuadTreeSector( this.Initial, other.Final );
			}
			else if ( IsMergeableNotInvertible( other, this, get, equals ) )
			{
				result = new QuadTreeSector( other.Initial, this.Final );
			}
			else
			{
				throw new Exception( $"Impossible merge sector {this} with {other}" );
			}

			return result;
		}

		public bool Equals( QuadTreeSector obj )
		{
			return Initial == obj.Initial && Final == obj.Final;
		}

		public static bool operator ==( QuadTreeSector a, QuadTreeSector b ) { return Equals( a, b ); }

		public static bool operator !=( QuadTreeSector a, QuadTreeSector b ) { return !Equals( a, b ); }
	}

	public static List<QuadTreeSector> QuadTree<T>( Func<int, int, T> get, Func<T, T, bool> equals, int size, bool mergeSectors )
	{
		return QuadTree( get, equals, new QuadTreeSector( 0, 0, size - 1, size - 1 ), mergeSectors );
	}

	public static List<QuadTreeSector> QuadTree<T>( Func<int, int, T> get, Func<T, T, bool> equals, QuadTreeSector sector, bool mergeSectors )
	{
		List<QuadTreeSector> result = new List<QuadTreeSector>();
		List<QuadTreeSector> currentSectors = new List<QuadTreeSector>();
		int firstCol = sector.Initial.x;
		int firstRow = sector.Initial.y;
		Vector2Int sectorSize = sector.Size;

		// If not square sector
		if ( sectorSize.x != sectorSize.y )
		{
			throw new Exception( $"ERROR QuadTree : Sector [ {sector} ] isn't a square" );
		}
		else if ( !IsPowerOfTwo( sectorSize.x ) )
		{
			throw new Exception( $"ERROR QuadTree : Sector size [ {sectorSize.x} ] isn't power of two" );
		}
		// If only one tile
		else if ( sectorSize.x == 1 )
		{
			result.Add( sector );
		}
		// Else, check the four sectors
		else
		{
			int midSectorColumns = ( sectorSize.x - 1 ) / 2;
			int midSectorRows = ( sectorSize.y - 1 ) / 2;
			int midCol = firstCol + midSectorColumns;
			int midRow = firstRow + midSectorRows;
			int finalCol = sector.Final.x;
			int finalRow = sector.Final.y;

			// Top-left
			result.AddRange( QuadTreeSectorCheck( new QuadTreeSector( firstCol, firstRow, midCol, midRow ), get, equals, mergeSectors ) );

			// Top-right
			currentSectors = QuadTreeSectorCheck( new QuadTreeSector( midCol + 1, firstRow, finalCol, midRow ), get, equals, mergeSectors );
			if ( mergeSectors ) MergeQuadTreeSectorsPositive( result, currentSectors, get, equals );
			else result.AddRange( currentSectors );

			// Bottom-left
			currentSectors = QuadTreeSectorCheck( new QuadTreeSector( firstCol, midRow + 1, midCol, finalRow ), get, equals, mergeSectors );
			if ( mergeSectors ) MergeQuadTreeSectorsPositive( result, currentSectors, get, equals );
			else result.AddRange( currentSectors );

			// Bottom-right ( not mergeable with Top-left )
			currentSectors = QuadTreeSectorCheck( new QuadTreeSector( midCol + 1, midRow + 1, finalCol, finalRow ), get, equals, mergeSectors );
			if ( mergeSectors ) MergeQuadTreeSectorsPositive( result, currentSectors, get, equals );
			else result.AddRange( currentSectors );
		}

		return result;
	}

	private static List<QuadTreeSector> QuadTreeSectorCheck<T>( QuadTreeSector sector, Func<int, int, T> get, Func<T, T, bool> equals, bool mergeSectors )
	{
		List<QuadTreeSector> result = new List<QuadTreeSector>();

		if ( IsEqualQuadTreeSector( sector, get, equals ) )
		{
			result.Add( sector );
		}
		else
		{
			result.AddRange( QuadTree( get, equals, sector, mergeSectors ) );
		}

		return result;
	}

	private static bool IsEqualQuadTreeSector<T>( QuadTreeSector sector, Func<int, int, T> get, Func<T, T, bool> equals )
	{
		bool equalGroup = true;

		T lastElement = get( sector.Initial.x, sector.Initial.y );
		T currentElement;
		for ( int col = sector.Initial.x; col <= sector.Final.x && equalGroup; col++ )
		{
			for ( int row = sector.Initial.y; row <= sector.Final.y && equalGroup; row++ )
			{
				currentElement = get( col, row );
				equalGroup &= equals( lastElement, currentElement );
			}
		}

		return equalGroup;
	}

	private static void MergeQuadTreeSectorsPositive<T>( List<QuadTreeSector> baseSectors, List<QuadTreeSector> greatersSectors, Func<int, int, T> get, Func<T, T, bool> equals )
	{
		QuadTreeSector baseSector;
		bool isMerged;

		foreach ( QuadTreeSector greaterSector in greatersSectors )
		{
			isMerged = false;
			for ( int j = 0; j < baseSectors.Count && !isMerged; j++ )
			{
				baseSector = baseSectors[j];

				if ( QuadTreeSector.IsMergeableNotInvertible( baseSector, greaterSector, get, equals ) )
				{
					baseSectors[j] = new QuadTreeSector( baseSector.Initial, greaterSector.Final );
					isMerged = true;
				}
			}
			if ( !isMerged ) baseSectors.Add( greaterSector );
		}
	}

	#endregion

	#region Common	

	public static T Max<T>( params T[] elementsList ) where T : IComparable<T>
	{
		T maximum = elementsList[0];
		T aux;

		for ( int i = 0; i < elementsList.Length; i++ )
		{
			aux = elementsList[i];
			if ( aux.CompareTo( maximum ) > 0 )
			{
				maximum = aux;
			}
		}

		return maximum;
	}

	public static T Min<T>( params T[] elementsList ) where T : IComparable<T>
	{
		T maximum = elementsList[0];
		T aux;

		for ( int i = 0; i < elementsList.Length; i++ )
		{
			aux = elementsList[i];
			if ( aux.CompareTo( maximum ) < 0 )
			{
				maximum = aux;
			}
		}

		return maximum;
	}

	public static T Clamp<T>( T value, T inclusiveMinimum, T inclusiveMaximum ) where T : IComparable<T>
	{
		T result = value;

		if ( result.CompareTo( inclusiveMaximum ) > 0 ) { result = inclusiveMaximum; }
		else if ( result.CompareTo( inclusiveMinimum ) < 0 ) { result = inclusiveMinimum; }

		return result;
	}

	public static bool IsPowerOfTwo( int value )
	{
		return ( value != 0 ) && ( ( value & ( value - 1 ) ) == 0 );
	}

	public static bool IsPossibleDirection( Vector2Int origin, Vector2Int direction, IsPositionAccesible isPositionAccesible )
	{
		bool possible = true;
		direction.TransformToDirection();

		Vector2Int[] movementComponents = {
			origin + Vector2Int.right * direction.x,
			origin + Vector2Int.up * direction.y,
			origin + direction
		};

		for ( int i = 0; i < movementComponents.Length && possible; i++ )
		{
			if ( !movementComponents[i].Equals( origin ) )
			{
				possible &= isPositionAccesible( movementComponents[i] );
			}
		}

		return possible;
	}

	// TODO : Add a version with min square radius
	public static List<Vector2Int> NearlyPositions( Vector2Int center, uint squareRadius )
	{
		List<Vector2Int> positions = new List<Vector2Int>();

		Vector2Int areaTopLeftCorner = center + Vector2Int.one * -1 * (int)squareRadius;
		Vector2Int position;
		for ( int x = 0; x <= squareRadius * 2; x++ )
		{
			for ( int y = 0; y <= squareRadius * 2; y++ )
			{
				position = areaTopLeftCorner + new Vector2Int( x, y );
				if ( position != center )
				{
					positions.Add( position );
				}
			}
		}

		return positions;
	}

	public static Vector2Int DirToTargetInRadius( Vector2Int origin, Vector2Int target, uint squareRadius )
	{
		Vector2Int dirToTarget = Vector2Int.zero;
		bool positionFound = false;
		Vector2Int areaTopLeftCorner = origin + Vector2Int.one * -1 * (int)squareRadius;

		Vector2Int pos;
		Vector2Int offset;
		for ( int x = 0; x <= squareRadius * 2 && !positionFound; x++ )
		{
			for ( int y = 0; y <= squareRadius * 2 && !positionFound; y++ )
			{
				offset = new Vector2Int( x, y );
				pos = areaTopLeftCorner + offset;
				if ( pos == target )
				{
					dirToTarget = pos - origin;
					positionFound = false;
				}
			}
		}

		return dirToTarget;
	}

	public static bool IsTouchingTarget( Vector2Int origin, Vector2Int target, IsPositionAccesible isPositionAccesible )
	{
		bool isTouching = false;

		if ( origin.Equals( target ) )
		{
			isTouching = true;
		}
		else
		{
			Vector2Int direction = DirToTargetInRadius( origin, target, 1 );

			if ( direction != Vector2Int.zero )
			{
				isTouching = true;

				// If is a diagonal
				if ( direction.x != 0 && direction.y != 0 )
				{
					Vector2Int[] movementComponents = { origin + Vector2Int.right * direction.x, origin + Vector2Int.up * direction.y };

					for ( int i = 0; i < movementComponents.Length && isTouching; i++ )
					{
						isTouching &= isPositionAccesible( movementComponents[i] );
					}
				}
			}
		}

		return isTouching;
	}

	#endregion

	#region Extended methods

	public static float Distance( this Vector2Int a, Vector2Int b )
	{
		return ( a - b ).magnitude;
	}

	public static void TransformToDirection( this Vector2Int vector )
	{
		vector.x = Clamp( vector.x, -1, 1 );
		vector.y = Clamp( vector.y, -1, 1 );
	}

	#endregion
}