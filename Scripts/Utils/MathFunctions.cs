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
	private static int NumMeasuresTimeAstar = 0;

	public static float AveragePathlengthAstar { get; private set; }
	public static float MaxPathlengthAstar { get; private set; }
	private static int NumMeasuresPathlengthAstar = 0;

	public static float AverageCheckedPositionsAstar { get; private set; }
	public static float MaxCheckedPositionsAstar { get; private set; }
	private static int NumMeasuresCheckedPositionsAstar = 0;

	private static void UpdateAstarStatistics( long time, int pathLength, int checkedPositions )
	{
		AverageTimeAstar = ( AverageTimeAstar * NumMeasuresTimeAstar + time ) / ( NumMeasuresTimeAstar + 1 );
		NumMeasuresTimeAstar++;
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

	/// <summary>
	/// Reset all the current statistics of MathFunctions.
	/// </summary>
	public static void ResetStatistics()
	{
		AverageTimeAstar = 0;
		MaxTimeAstar = 0;
		NumMeasuresTimeAstar = 0;

		AveragePathlengthAstar = 0;
		MaxPathlengthAstar = 0;
		NumMeasuresPathlengthAstar = 0;

		AverageCheckedPositionsAstar = 0;
		MaxCheckedPositionsAstar = 0;
		NumMeasuresCheckedPositionsAstar = 0;
	}

	/// <summary>
	/// <para>Get a string with the current MathFunctions statistics.</para>
	/// <para>Includes: A* </para>
	/// </summary>
	/// <returns>String with the statistics info.</returns>
	public static string GetStatistics()
	{
		return $"A* statistics:" +
					$"\nNum calls = {NumMeasuresTimeAstar}" +
					$"\nAverage time = {AverageTimeAstar} ms" +
					$"\nMax time = {MaxTimeAstar} ms" +
					$"\nAverage path length = {AveragePathlengthAstar}" +
					$"\nMax path length = {MaxPathlengthAstar}" +
					$"\nAverage checked positions = {AverageCheckedPositionsAstar}" +
					$"\nMax checked positions = {MaxCheckedPositionsAstar}" +
					$"\nRelation path length <-> checked positions = {( AveragePathlengthAstar / AverageCheckedPositionsAstar ).ToString( "f3" )}";
		// TODO : Implement direct path statistics
	}

	#endregion

	#region Pathfinding

	/// <summary>
	/// Method that checks if a position is accessible.
	/// </summary>
	/// <param name="position">Position to check.</param>
	/// <returns>If the position is accessible.</returns>
	public delegate bool IsPositionAccessible( Vector2Int position );

	/// <summary>
	/// <para>Obtains a path from origin to objective reusing the lastPathToObjective if is possible ( if origin and objective exists in path one before the other ).</para>
	/// <para>If is not possible, uses the method MathFunctions.Pathfinding to calculate a new path. See specific documentation of MathFunctions.Pathfinding for more information.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="lastPathToObjective">Last path to objective calculated. Checked for reuse.</param>
	/// <param name="maxPathLenght">Maximum length of the path if it must be recalculated and cannot be direct.</param>
	/// <returns>List of the positions of the best path found. Not includes the origin position.</returns>
	public static List<Vector2Int> PathfindingWithReusing( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible, List<Vector2Int> lastPathToObjective, int maxPathLenght )
	{
		List<Vector2Int> path = new List<Vector2Int>();

		// If any calculated path
		if ( lastPathToObjective == null || lastPathToObjective.Count == 0 )
		{
			path = Pathfinding( origin, objective, isPositionAccessible, maxPathLenght );
		}
		// Is some path previously calculated
		else
		{
			int pos;
			bool isPathClear = true;
			bool pathContainsOrigin = false;
			int originPosInPath = -1;
			bool pathContainsObjective = false;
			Vector2Int currentposition;

			// Check is the path is clear and if some position is already done
			for ( pos = 0; pos < lastPathToObjective.Count && isPathClear && !pathContainsObjective; pos++ )
			{
				currentposition = lastPathToObjective[pos];

				isPathClear = isPositionAccessible( currentposition );

				// Check if pos is origin
				if ( !pathContainsOrigin && currentposition == origin )
				{
					pathContainsOrigin = true;
					originPosInPath = pos;
				}
				// Check if is target only is origin has been found
				else
				{
					pathContainsObjective = currentposition == objective;
				}
			}

			// If the last path can be used
			if ( pathContainsOrigin && pathContainsObjective )
			{
				path = lastPathToObjective.GetRange( 0, pos - 1 );
			}
			// If is impossible to reuse path
			else
			{
				path = Pathfinding( origin, objective, isPositionAccessible, maxPathLenght );
			}
		}

		return path;
	}

	/// <summary>
	/// <para>Obtains the path from the origin to objective.</para>
	/// <para>First will search a direct path to objective and, if is not possible, will use the A* algorithm.</para>
	/// <para>If the path to objective is impossible, returns a path to the position closest to the target.</para>
	/// <para>Start position will not be included in result path.</para>
	/// <para>maxPathLenght will be used only if a direct path to target cannot be found.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="maxPathLenght">Maximum path lenght of a non-direct path.</param>
	/// <returns>List of the positions of the best path found. Not includes the origin position.</returns>
	public static List<Vector2Int> Pathfinding( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible, int maxPathLenght )
	{
		List<Vector2Int> path;

		// First, search direct path
		path = DirectPath( origin, objective, isPositionAccessible );

		// If the direct path not arrives to target
		if ( path.Count == 0 || !IsTouchingObjective( path[path.Count - 1], objective, isPositionAccessible ) )
		{
			path = AstarPath( origin, objective, isPositionAccessible, maxPathLenght );
		}

		return path;
	}

	/// <summary>
	/// Auxiliar class to calculate costs and link positions of a path.
	/// </summary>
	private class PositionInPath
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
	/// <summary>
	/// <para>A* pathfinding algorithm. Calculates the shortest path from origin to target if exists.</para>
	/// <para>If the number of checked positions is greater than maxCheckedPositions or the objective is not accessible, returns a path to the position closest to the target.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="maxCheckedPositions">Maximum number of checked positions to reach the objective.</param>
	/// <returns>The best path found from origin to objective.</returns>
	public static List<Vector2Int> AstarPath( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible, int maxCheckedPositions )
	{
		Stopwatch chrono = new Stopwatch();
		chrono.Start();

		List<Vector2Int> path = new List<Vector2Int>();
		List<PositionInPath> checkedPositions = new List<PositionInPath>();
		List<PositionInPath> remainingPositions = new List<PositionInPath>() { new PositionInPath( origin, 0, null ) };

		PositionInPath currentPositionInPath = null;
		PositionInPath closestToObjective = remainingPositions[0];
		float closestDistanceToObjective = closestToObjective.Position.Distance( objective );
		PositionInPath nextPositionInPath;
		float auxDistance;

		bool destinyNotAccessible = false;
		bool objectiveReached = false;

		// Calcule path 
		while ( !objectiveReached && !destinyNotAccessible )
		{
			// Select next position
			currentPositionInPath = remainingPositions[0];
			foreach ( PositionInPath positionInPath in remainingPositions )
			{
				if ( ( positionInPath.Position.Distance( objective ) + positionInPath.AcummulatedCost ) <
					( currentPositionInPath.Position.Distance( objective ) + currentPositionInPath.AcummulatedCost ) )
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

				// If the position isn't checked and is accessible
				if ( !checkedPositions.Contains( nextPositionInPath ) && IsPossibleDirection( currentPositionInPath.Position, movement, isPositionAccessible ) )
				{
					remainingPositions.Add( nextPositionInPath );

					// Check closest to target
					auxDistance = nextPositionInPath.Position.Distance( objective );
					if ( auxDistance < closestDistanceToObjective )
					{
						closestToObjective = nextPositionInPath;
						closestDistanceToObjective = auxDistance;
					}
				}
			}

			// Check end of path
			if ( IsTouchingObjective( currentPositionInPath.Position, objective, isPositionAccessible ) )
			{
				objectiveReached = true;
			}
			else
			{
				destinyNotAccessible = remainingPositions.Count == 0 || checkedPositions.Count > maxCheckedPositions;
			}
		}

		// If is impossible to acces to the target use the closest position as desitiny
		if ( destinyNotAccessible )
		{
			currentPositionInPath = closestToObjective;
		}

		// If the currentPosition ( best last position found ) is closer to target than the origin is a good path
		if ( currentPositionInPath.Position.Distance( objective ) < origin.Distance( objective ) )
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

	/// <summary>
	/// <para>Calculates the direct path ( straight or diagonal line ) if is possible or return a incomplete path.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>A direct path to objective if is possible, or a incomplete path instead.</returns>
	public static List<Vector2Int> DirectPath( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible )
	{
		List<Vector2Int> path = new List<Vector2Int>();

		// TODO : Best control of blocked targets
		if ( !IsObjectiveAccessible( objective, isPositionAccessible ) )
		{
			Vector2Int position = origin;
			Vector2Int movement = new Vector2Int();

			bool targetNotAccessible = false;

			while ( !position.Equals( objective ) && !targetNotAccessible )
			{
				movement.x = -Clamp( position.x.CompareTo( objective.x ), -1, 1 );
				movement.y = -Clamp( position.y.CompareTo( objective.y ), -1, 1 );

				if ( IsPossibleDirection( position, movement, isPositionAccessible ) )
				{
					position = position + movement;
					path.Add( position );
				}
				else
				{
					targetNotAccessible = true;
				}
			}
		}

		return path;
	}

	/// <summary>
	/// Checks if the objective is accessible by at least one of the surrounding positions ( in eight directions ).
	/// </summary>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>True only if the objective can be accesed by at least one surrounding position.</returns>
	public static bool IsObjectiveAccessible( Vector2Int objective, IsPositionAccessible isPositionAccessible )
	{
		bool isAccessible = false;

		// For all directions, while a acces to the target isn't found
		for ( int i = 0; i < EightDirections2D.Count && !isAccessible; i++ )
		{
			isAccessible = isPositionAccessible( objective + EightDirections2D[i] );
		}

		return isAccessible;
	}

	/// <summary>
	/// <para>Checks if a movement from a origin to a specific direction ( in some of the eight possible directions ) is possible.</para>
	/// <para>To check it the method observe the adjacents possitions, avoiding a movement that go throught a inaccesible position.</para>
	/// </summary>
	/// <param name="origin">Original position.</param>
	/// <param name="direction">Direction of the movement.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>If is a possible direction.</returns>
	public static bool IsPossibleDirection( Vector2Int origin, Vector2Int direction, IsPositionAccessible isPositionAccessible )
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
				possible &= isPositionAccessible( movementComponents[i] );
			}
		}

		return possible;
	}

	// TODO : Add a version with min square radius
	/// <summary>
	/// Obtains the adjacent positions in a square radius, excluding the center.
	/// </summary>
	/// <param name="center">Center of the square area.</param>
	/// <param name="squareRadius">Radius of the square area.</param>
	/// <returns>List of the positions in the square area.</returns>
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

	/// <summary>
	/// Checks if the objective is accesible from the origin position in a single movement.
	/// </summary>
	/// <param name="origin">Original position.</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>If the objective is accesible from the origin position in a single movement.</returns>
	public static bool IsTouchingObjective( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible )
	{
		bool isTouching = false;

		if ( origin.Equals( objective ) )
		{
			isTouching = true;
		}
		else
		{
			Vector2Int direction = DirToObjectiveInRadius( origin, objective, 1 );

			if ( direction != Vector2Int.zero )
			{
				isTouching = true;

				// If is a diagonal
				if ( direction.x != 0 && direction.y != 0 )
				{
					Vector2Int[] movementComponents = { origin + Vector2Int.right * direction.x, origin + Vector2Int.up * direction.y };

					for ( int i = 0; i < movementComponents.Length && isTouching; i++ )
					{
						isTouching &= isPositionAccessible( movementComponents[i] );
					}
				}
			}
		}

		return isTouching;
	}

	/// <summary>
	/// <para>Obtains the direction form origin to objective only if the objective is in the square radius.</para>
	/// <para>If the objective is not found, returns the direction (0,0).</para>
	/// </summary>
	/// <param name="origin">Original position and center of the square area.</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="squareRadius">Radius of the square area.</param>
	/// <returns>The direction to the objective if it is in the square area. Otherwise, the direction (0,0) is returned.</returns>
	private static Vector2Int DirToObjectiveInRadius( Vector2Int origin, Vector2Int objective, uint squareRadius )
	{
		Vector2Int dirToObjective = Vector2Int.zero;
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
				if ( pos == objective )
				{
					dirToObjective = pos - origin;
					positionFound = false;
				}
			}
		}

		return dirToObjective;
	}

	#endregion

	#region Random generation

	/// <summary>
	/// Generates a random float matrix using perlin noise.
	/// </summary>
	/// <param name="columns">Number of columns of the result matrix.</param>
	/// <param name="rows">Number of rows of the result matrix.</param>
	/// <param name="seed">Seed of the result. Preferaby not integer.</param>
	/// <param name="octaves"></param>	// TODO
	/// <param name="persistance"></param> 	// TODO
	/// <param name="lacunarity"></param> 	// TODO
	/// <param name="minValue">Minimum value. Infinity not allowed.</param>
	/// <param name="maxValue">Maximum value. Infinity not allowed.</param>
	/// <returns>A random float matrix with the columns and rows specified.</returns>
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

	/// <summary>
	/// <para>Calculates a pseudo-random direction based on last direction and probability.</para>
	/// <para>If the selected direction is impossible, choose other randomly.</para>
	/// <para>If any direction is possible, returns Vector2Int.zero.</para>
	/// </summary>
	/// <param name="origin">Original position.</param>
	/// <param name="lastDirection">Last direction. Must be a normalized vector.</param>
	/// <param name="randGenerator">Random generator for the direction.</param>
	/// <param name="sameDirectionProbability">Probability of conserve the lastDirection.</param>
	/// <param name="isPositionAccessible">>Method to check if a position is accessible.</param>
	/// <returns>The new direction or, if any is possible, Vector2Int.zero.</returns>
	public static Vector2Int PseudorandomDirection( Vector2Int origin, Vector2Int lastDirection, System.Random randGenerator, int sameDirectionProbability, IsPositionAccessible isPositionAccessible )
	{
		Vector2Int nextDirection = Vector2Int.zero;
		lastDirection.TransformToDirection();

		int sameDirectionProb = Clamp( sameDirectionProbability, 0, 100 );
		bool useSameDirection = randGenerator.Next( 0, 100 ) <= sameDirectionProb;

		// If is possible continue in the same direction ( not 0,0 )
		if ( !lastDirection.Equals( Vector2Int.zero ) && useSameDirection && IsPossibleDirection( origin, lastDirection, isPositionAccessible ) )
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
			while ( directions.Count > 0 && nextDirection == Vector2Int.zero )
			{
				possibleDirection = directions[randGenerator.Next( 0, directions.Count )];

				if ( IsPossibleDirection( origin, possibleDirection, isPositionAccessible ) )
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
			if ( nextDirection.Equals( Vector2Int.zero ) && IsPossibleDirection( origin, possibleDirection, isPositionAccessible ) )
			{
				nextDirection = possibleDirection;
			}
		}

		return nextDirection;
	}

	#endregion

	#region Matrix and array scaling up with interpolation

	/// <summary>
	/// <para>Scales up a generic matrix obtaining the intermediate values by interpolation.</para>
	/// <para>Obtains the scale up factor using the resultDim by originalDim division. Divible dimensions are recomended ( to avoid repeated values in borders ), but not required.</para>
	/// <para>Internally uses MathFunctions.ScaleUpMatrixValue.</para>
	/// </summary>
	/// <typeparam name="T">Type of the elements of the matrix.</typeparam>
	/// <param name="get">Getter of values of the original matrix.</param>
	/// <param name="originalDim">Dimension ( columns and rows ) of the original matrix.</param>
	/// <param name="resultDim">Dimension ( columns and rows ) of the result matrix.</param>
	/// <param name="multiplyFunc">Multiplication function for the interpolation. Must accept multiplication by values between 0 and 1 (both included).</param>
	/// <param name="addFunc">Add function for the interpolation.</param>
	/// <returns>The scaled up matrix.</returns>
	public static T[,] ScaleUpMatrix<T>( Func<int, int, T> get, Vector2Int originalDim, Vector2Int resultDim, Func<T, float, T> multiplyFunc, Func<T, T, T> addFunc )
	{
		T[,] result = new T[resultDim.x, resultDim.y];
		int scaleUp = (int)( ( (float)resultDim.x / originalDim.x + (float)resultDim.y / originalDim.y ) / 2f );

		for ( int i = 0; i < resultDim.x; i++ )
		{
			for ( int j = 0; j < resultDim.y; j++ )
			{
				result[i, j] = ScaleUpMatrixValue( get, scaleUp, i, j, originalDim, multiplyFunc, addFunc );
			}
		}

		return result;
	}

	/// <summary>
	/// Scales up a specific value of a generic matrix calculating the interpolation.
	/// </summary>
	/// <typeparam name="T">Type of the elements of the generic matrix.</typeparam>
	/// <param name="get">Getter of values of the original matrix.</param>
	/// <param name="scaleUpFactor">Multiply factor of the original dimension.</param>
	/// <param name="col">Column of the value to calculate.</param>
	/// <param name="row">Row of the value to calculate.</param>
	/// <param name="originalDim">Dimension ( columns and rows ) of the original matrix.</param>
	/// <param name="multiplyFunc">Multiplication function for the interpolation. Must accept multiplication by values between 0 and 1 (both included).</param>
	/// <param name="addFunc">Add function for the interpolation.</param>
	/// <returns>The scaled up value.</returns>
	public static T ScaleUpMatrixValue<T>( Func<int, int, T> get, int scaleUpFactor, int col, int row, Vector2Int originalDim, Func<T, float, T> multiplyFunc, Func<T, T, T> addFunc )
	{
		float originalCol = Clamp( col / (float)scaleUpFactor, 0, originalDim.x - 1 );
		float originalRow = Clamp( row / (float)scaleUpFactor, 0, originalDim.y - 1 );
		return InterpolateValueInMatrix( get, originalCol, originalRow, multiplyFunc, addFunc );
	}

	/// <summary>
	/// Interpolates the value of a decimal position ( column and row ) in a matrix.
	/// </summary>
	/// <typeparam name="T">Type of the elements of the matrix.</typeparam>
	/// <param name="get">Getter of values of the matrix.</param>
	/// <param name="col">Decimal column of the value to interpolate.</param>
	/// <param name="row">Decimal row of the value to interpolate.</param>
	/// <param name="multiplyFunc">Multiplication function for the interpolation. Must accept multiplication by values between 0 and 1 (both included).</param>
	/// <param name="addFunc">Add function for the interpolation.</param>
	/// <returns>The interpolated value.</returns>
	public static T InterpolateValueInMatrix<T>( Func<int, int, T> get, float col, float row, Func<T, float, T> multiplyFunc, Func<T, T, T> addFunc )
	{
		T result;

		int matrixIntCol = (int)col;
		float matrixDecimalCol = col - matrixIntCol;

		int matrixIntRow = (int)row;
		float matrixDecimalRow = row - matrixIntRow;

		result = multiplyFunc( multiplyFunc( get( matrixIntCol, matrixIntRow ), 1f - matrixDecimalCol ), 1f - matrixDecimalRow );
		if ( matrixDecimalCol > 0 )
			result = addFunc( result, multiplyFunc( multiplyFunc( get( matrixIntCol + 1, matrixIntRow ), matrixDecimalCol ), 1f - matrixDecimalRow ) );
		if ( matrixDecimalRow > 0 )
			result = addFunc( result, multiplyFunc( multiplyFunc( get( matrixIntCol, matrixIntRow + 1 ), 1f - matrixDecimalCol ), matrixDecimalRow ) );
		if ( matrixDecimalCol > 0 && matrixDecimalRow > 0 )
			result = addFunc( result, multiplyFunc( multiplyFunc( get( matrixIntCol + 1, matrixIntRow + 1 ), matrixDecimalCol ), matrixDecimalRow ) );

		return result;
	}

	/// <summary>
	/// Interpolates the value of a decimal position in a array.
	/// </summary>
	/// <typeparam name="T">Type of the elements of the array.</typeparam>
	/// <param name="get">Getter of values of the array.</param>
	/// <param name="decimalIndex">Decimal position of the value to interpolate.</param>
	/// <param name="multiplyFunc">Multiplication function for the interpolation. Must accept multiplication by values between 0 and 1 (both included).</param>
	/// <param name="addFunc">Add function for the interpolation.</param>
	/// <returns></returns>
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

	/// <summary>
	/// Struct to store the fundamental information of a sector: initial and final position as a quadrilateral.
	/// </summary>
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

		public QuadTreeSector( int initialX, int initialY, int finalX, int finalY )
		{
			Initial = new Vector2Int( initialX, initialY );
			Final = new Vector2Int( finalX, finalY );
		}

		public override string ToString()
		{
			return Initial + " " + Final;
		}

		/// <summary>
		/// <para>Checks if a sector is mergeable with other assuming that the other is positioned in at positive direction ( right, bottom or both ).</para>
		/// <para>Therefore, if a sector if mergeable with other, according to this method the same sectors inverted will not be mergeables.</para>
		/// </summary>
		/// <typeparam name="T">Type of the quadtree matrix values.</typeparam>
		/// <param name="original">Original sector.</param>
		/// <param name="other">Other sector.</param>
		/// <param name="get">Getter of the quadtree matrix values.</param>
		/// <param name="equals">Comparator of quadtree matrix elements. Must return true if a value is compared with himself.</param>
		/// <returns>If the original sector is mergeable with other assuming that the other is positioned in at positive direction ( right, bottom or both ).</returns>
		public static bool AreMergeableNotInvertible<T>( QuadTreeSector original, QuadTreeSector other, Func<int, int, T> get, Func<T, T, bool> equals )
		{
			// TODO : Check if is really mergeable when contained
			bool notContained = original.Initial != other.Initial && original.Final != other.Final && original.Initial != other.Final && original.Final != other.Initial;
			bool horitzontalMatch = original.Final.x == other.Initial.x - 1 && original.Final.y == other.Final.y && original.Initial.y == other.Initial.y;
			bool verticalMatch = original.Final.y == other.Final.y - 1 && original.Final.x == other.Final.x && original.Initial.x == other.Initial.x;
			bool equalsValues = equals( get( other.Initial.x, other.Initial.y ), get( original.Initial.x, original.Initial.y ) );
			return notContained && ( horitzontalMatch || verticalMatch ) && equalsValues;
		}

		/// <summary>
		/// Tries a merge between the current sector and other. If the merge is not possible, throws a Exception.
		/// </summary>
		/// <typeparam name="T">Type of the quadtree matrix values.</typeparam>
		/// <param name="other">Other sector.</param>
		/// <param name="get">Getter of the quadtree matrix values.</param>
		/// <param name="equals">Comparator of quadtree matrix elements. Must return true if a value is compared with himself.</param>
		/// <returns>The merged sector if is possible. Else, throws a exception.</returns>
		public QuadTreeSector TryMerge<T>( QuadTreeSector other, Func<int, int, T> get, Func<T, T, bool> equals )
		{
			QuadTreeSector result;

			if ( AreMergeableNotInvertible( this, other, get, equals ) )
			{
				result = new QuadTreeSector( this.Initial, other.Final );
			}
			else if ( AreMergeableNotInvertible( other, this, get, equals ) )
			{
				result = new QuadTreeSector( other.Initial, this.Final );
			}
			else
			{
				throw new Exception( $"Impossible merge sector {this} with {other}" );
			}

			return result;
		}

		public override bool Equals( object obj )
		{
			QuadTreeSector? other = obj as QuadTreeSector?;
			return other != null && Initial == other.Value.Initial && Final == other.Value.Final;
		}

		public override int GetHashCode()
		{
			return Initial.GetHashCode() + Final.GetHashCode();
		}

		public static bool operator ==( QuadTreeSector a, QuadTreeSector b ) { return a.Equals( b ); }

		public static bool operator !=( QuadTreeSector a, QuadTreeSector b ) { return !a.Equals( b ); }
	}

	/// <summary>
	/// Calculates the quadtree sectors of a square matrix with a power of two dimensions ( columns and rows ).
	/// </summary>
	/// <typeparam name="T">Type of the matrix values.</typeparam>
	/// <param name="get">Getter of the matrix values.</param>
	/// <param name="equals">Comparator of matrix elements. Must return true if a value is compared with himself.</param>
	/// <param name="size">Size of the matrix ( columns and rows ). Must be power of two or a exception will be thrown.</param>
	/// <param name="mergeSectors">If is true : some of the mergeable sectors ( equals and have a common size ) will be merged. Implies an additional cost but fewer sectors will be generated.</param>
	/// <returns>List of the obtained sectors.</returns>
	public static List<QuadTreeSector> QuadTree<T>( Func<int, int, T> get, Func<T, T, bool> equals, int size, bool mergeSectors )
	{
		return QuadTree( get, equals, new QuadTreeSector( 0, 0, size - 1, size - 1 ), mergeSectors );
	}

	/// <summary>
	/// <para>Calculates the quadtree sectors of a square matrix sector with a power of two dimensions ( columns and rows ).</para>
	/// <para>Is recursive.</para>
	/// </summary>
	/// <typeparam name="T">Type of the matrix values.</typeparam>
	/// <param name="get">Getter of the matrix values.</param>
	/// <param name="equals">Comparator of matrix elements. Must return true if a value is compared with himself.</param>
	/// <param name="sector">Sector to apply the quadtree algorithm.</param>
	/// <param name="mergeSectors">If is true : some of the mergeable sectors ( equals and have a common size ) will be merged. Implies an additional cost but fewer sectors will be generated.</param>
	/// <returns>List of the obtained sectors.</returns>
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

	/// <summary>
	/// Checks a quadtree sector. Test if the sector is equal and calls MathFunctions.QuadTree if it isn't.
	/// </summary>
	/// <typeparam name="T">Type of the matrix values.</typeparam>
	/// <param name="sector">Sector to check.</param>
	/// <param name="get">Getter of the matrix values.</param>
	/// <param name="equals">Comparator of matrix elements. Must return true if a value is compared with himself.</param>
	/// <param name="mergeSectors">If the sectors will be merged. Used to pass it to MathFunctions.QuadTree if is called.</param>
	/// <returns>List of the obtained sectors.</returns>
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

	/// <summary>
	/// Check if a quadtree sector is completly equal. For that the first position is compared with all the other positions.
	/// </summary>
	/// <typeparam name="T">Type of the matrix values.</typeparam>
	/// <param name="sector">Sector to check.</param>
	/// <param name="get">Getter of the matrix values.</param>
	/// <param name="equals">Comparator of matrix elements. Must return true if a value is compared with himself.</param>
	/// <returns>If the sector is completely equal.</returns>
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

	/// <summary>
	/// Merge a list of quadtree sectors with other list of sectors of a positive position ( more to the right or down ).
	/// </summary>
	/// <typeparam name="T">Type of the matrix values.</typeparam>
	/// <param name="baseSectors">Original list of sectors. Will be modified adding new sectors ( merged or not ).</param>
	/// <param name="greatersSectors">Sectors to try to merge or, instead, add to baseSectors.</param>
	/// <param name="get">Getter of the matrix values.</param>
	/// <param name="equals">Comparator of matrix elements. Must return true if a value is compared with himself.</param>
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

				if ( QuadTreeSector.AreMergeableNotInvertible( baseSector, greaterSector, get, equals ) )
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

	/// <summary>
	/// Founds the maximum value of a elements list.
	/// </summary>
	/// <typeparam name="T">Type that implements IComparable<T></typeparam>
	/// <param name="elementsList">List of elements/parameters.</param>
	/// <returns>The maxium value of elementsList.</returns>
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

	/// <summary>
	/// Founds the minimum value of a elements list.
	/// </summary>
	/// <typeparam name="T">Type that implements IComparable<T></typeparam>
	/// <param name="elementsList">List of elements/parameters.</param>
	/// <returns>The minimum value of elementsList.</returns>
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

	/// <summary>
	/// Clamps a value in a range.
	/// </summary>
	/// <typeparam name="T">Type that implements IComparable<T></typeparam>
	/// <param name="value">Value to clamp.</param>
	/// <param name="inclusiveMinimum">Minimum value of the range (included).</param>
	/// <param name="inclusiveMaximum">Maximum value of the range (included).</param>
	/// <returns>The value clamped in the range.</returns>
	public static T Clamp<T>( T value, T inclusiveMinimum, T inclusiveMaximum ) where T : IComparable<T>
	{
		T result = value;

		if ( result.CompareTo( inclusiveMaximum ) > 0 ) { result = inclusiveMaximum; }
		else if ( result.CompareTo( inclusiveMinimum ) < 0 ) { result = inclusiveMinimum; }

		return result;
	}

	/// <summary>
	/// Checks if a integer is power of two.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <returns>If the integer is power of two.</returns>
	public static bool IsPowerOfTwo( int value )
	{
		return ( value != 0 ) && ( ( value & ( value - 1 ) ) == 0 );
	}

	#endregion

	#region Extended methods

	/// <summary>
	/// Obtains the euclidean distance from current vector to other position.
	/// </summary>
	/// <param name="a">Current position.</param>
	/// <param name="b">Other position.</param>
	/// <returns>The euclidean distance between positions.</returns>
	public static float Distance( this Vector2Int a, Vector2Int b )
	{
		return ( b - a ).magnitude;
	}

	/// <summary>
	/// Transforms current vector to a discrete direction ( values in range [-1,1] ).
	/// </summary>
	/// <param name="vector">Vector transformed to direction.</param>
	public static void TransformToDirection( this Vector2Int vector )
	{
		vector.x = Clamp( vector.x, -1, 1 );
		vector.y = Clamp( vector.y, -1, 1 );
	}

	#endregion
}