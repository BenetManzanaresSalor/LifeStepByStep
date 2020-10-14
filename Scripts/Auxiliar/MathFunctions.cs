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
	/// <param name="maxCheckedPositions">Maximum number of checked positions for a non-direct path.</param>
	/// <returns>List of the positions of the best path found. Not includes the origin position.</returns>
	public static List<Vector2Int> Pathfinding( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible, int maxCheckedPositions )
	{
		// First, search direct path
		List<Vector2Int> path = DirectPath( origin, objective, isPositionAccessible );

		// If the direct path not arrives to target, use A*
		if ( path.Count == 0 || !IsTouchingObjective( path[path.Count - 1], objective, isPositionAccessible ) )
			path = AstarPath( origin, objective, isPositionAccessible, maxCheckedPositions );

		return path;
	}

	/// <summary>
	/// Auxiliar class to calculate costs and link positions of a path.
	/// </summary>
	protected class PositionInPath
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
				objectiveReached = true;
			else
				destinyNotAccessible = remainingPositions.Count == 0 || checkedPositions.Count > maxCheckedPositions;
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
				movement.x = -Mathf.Clamp( position.x.CompareTo( objective.x ), -1, 1 );
				movement.y = -Mathf.Clamp( position.y.CompareTo( objective.y ), -1, 1 );

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
			isAccessible = isPositionAccessible( objective + EightDirections2D[i] );

		return isAccessible;
	}

	/// <summary>
	/// <para>Checks if a movement from a origin with a specific direction ( in some of the eight possible directions ) is possible.</para>
	/// <para>To check it the method observe the adjacents positions, avoiding a movement that go throught a inaccesible position.</para>
	/// </summary>
	/// <param name="origin">Original position.</param>
	/// <param name="direction">Direction of the movement.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>If is a possible direction.</returns>
	public static bool IsPossibleDirection( Vector2Int origin, Vector2Int direction, IsPositionAccessible isPositionAccessible )
	{
		bool possible = true;
		direction = direction.TransformToDirection();

		Vector2Int[] movementComponents = {
			origin + Vector2Int.right * direction.x,
			origin + Vector2Int.up * direction.y,
			origin + direction
		};

		for ( int i = 0; i < movementComponents.Length && possible; i++ )
			if ( !movementComponents[i].Equals( origin ) )
				possible &= isPositionAccessible( movementComponents[i] );

		return possible;
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
			isTouching = true;
		else
		{
			Vector2Int direction = objective - origin;

			if ( Mathf.Abs( direction.x ) <= 1 && Mathf.Abs( direction.y ) <= 1 )
			{
				isTouching = true;

				// If is a diagonal
				if ( direction.x != 0 && direction.y != 0 )
					isTouching = isPositionAccessible( origin + Vector2Int.right * direction.x ) &&
						isPositionAccessible( origin + Vector2Int.up * direction.y );
			}
		}

		return isTouching;
	}

	/// <summary>
	/// Obtains the adjacent positions in a square radius, excluding the center.
	/// </summary>
	/// <param name="center">Center of the square area.</param>
	/// <param name="squareRadius">Radius of the square area.</param>
	/// <returns>List of the positions in the square area.</returns>
	public static List<Vector2Int> AroundPositions( Vector2Int center, uint squareRadius )
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

	#endregion

	#region Random generation

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
		lastDirection = lastDirection.TransformToDirection();

		int sameDirectionProb = Mathf.Clamp( sameDirectionProbability, 0, 100 );
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
					nextDirection = possibleDirection;
				else
					directions.Remove( possibleDirection );
			}

			// If any other direction isn't possible, check the opposite to last direction
			if ( nextDirection.Equals( Vector2Int.zero ) )
			{
				possibleDirection = lastDirection * -1;
				if ( IsPossibleDirection( origin, possibleDirection, isPositionAccessible ) )
					nextDirection = possibleDirection;
			}
		}

		return nextDirection;
	}

	public static double RandomDouble( System.Random randomGenerator, double minInclusive, double maxExclusive )
	{
		return minInclusive + randomGenerator.NextDouble() * ( maxExclusive - minInclusive );
	}

	#endregion

	#region Common

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
	public static Vector2Int TransformToDirection( this Vector2Int vector )
	{
		vector.x = Mathf.Clamp( vector.x, -1, 1 );
		vector.y = Mathf.Clamp( vector.y, -1, 1 );

		return vector;
	}

	public static Vector3 Div( this Vector3 a, Vector3 b )
	{
		return new Vector3( a.x / b.x, a.y / b.y, a.z / b.z );
	}

	#endregion
}