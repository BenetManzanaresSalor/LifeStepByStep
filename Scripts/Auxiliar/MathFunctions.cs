using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class MathFunctions
{
	#region Constants

	public static readonly List<Vector2Int> FourDirections2D = new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
	public static readonly List<Vector2Int> EightDirections2D = new List<Vector2Int>
		{ Vector2Int.up, Vector2Int.up + Vector2Int.right, Vector2Int.right, Vector2Int.right + Vector2Int.down, Vector2Int.down,
		Vector2Int.down + Vector2Int.left, Vector2Int.left,  Vector2Int.left + Vector2Int.up };

	#endregion

	#region Statistics

	public static int NumCallsAstar { get; private set; }
	public static long AverageTimeAstar { get; private set; }
	public static long MaxTimeAstar { get; private set; }
	public static float AveragePathLengthAstar { get; private set; }
	public static float MaxPathLengthAstar { get; private set; }
	public static float AverageCheckedPositionsAstar { get; private set; }
	public static float MaxCheckedPositionsAstar { get; private set; }

	private static void UpdateAstarStatistics( long time, int pathLength, int checkedPositions )
	{
		AverageTimeAstar = ( AverageTimeAstar * NumCallsAstar + time ) / ( NumCallsAstar + 1 );
		NumCallsAstar++;
		if ( time > MaxTimeAstar )
			MaxTimeAstar = time;

		AveragePathLengthAstar = ( AveragePathLengthAstar * NumCallsAstar + pathLength ) / ( NumCallsAstar + 1 );
		if ( pathLength > MaxPathLengthAstar )
			MaxPathLengthAstar = pathLength;

		AverageCheckedPositionsAstar = ( AverageCheckedPositionsAstar * NumCallsAstar + checkedPositions ) / ( NumCallsAstar + 1 );
		if ( checkedPositions > MaxCheckedPositionsAstar )
			MaxCheckedPositionsAstar = checkedPositions;
	}

	/// <summary>
	/// Reset all the current statistics of MathFunctions.
	/// </summary>
	public static void ResetStatistics()
	{
		NumCallsAstar = 0;

		AverageTimeAstar = 0;
		MaxTimeAstar = 0;

		AveragePathLengthAstar = 0;
		MaxPathLengthAstar = 0;

		AverageCheckedPositionsAstar = 0;
		MaxCheckedPositionsAstar = 0;
	}

	/// <summary>
	/// <para>Get a string with the current MathFunctions statistics.</para>
	/// <para>Includes: A* </para>
	/// </summary>
	/// <returns>String with the statistics info.</returns>
	public static string GetStatistics()
	{
		return $"A* statistics:" +
					$"\nNum calls = {NumCallsAstar}" +
					$"\nAverage time = {AverageTimeAstar} ms" +
					$"\nMax time = {MaxTimeAstar} ms" +
					$"\nAverage path length = {AveragePathLengthAstar}" +
					$"\nMax path length = {MaxPathLengthAstar}" +
					$"\nAverage checked positions = {AverageCheckedPositionsAstar}" +
					$"\nMax checked positions = {MaxCheckedPositionsAstar}" +
					$"\nRelation path length <-> checked positions = {( AveragePathLengthAstar / AverageCheckedPositionsAstar ).ToString( "f3" )}";
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
	/// <para>Obtains the path from the origin to target.</para>
	/// <para>First will search a direct path to target and, if is not possible, will use the A* algorithm.</para>
	/// <para>If the path to target is impossible, returns a path to the position closest to the objective.</para>
	/// <para>Start position will not be included in result path.</para>
	/// <para>maxPathLenght will be used only if a direct path to target cannot be found.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="maxCheckedPositions">Maximum number of checked positions for a non-direct path.</param>
	/// <returns>List of the positions of the best path found. Not includes the origin position.</returns>
	public static List<Vector2Int> Pathfinding( Vector2Int origin, Vector2Int target, IsPositionAccessible isPositionAccessible, int maxCheckedPositions )
	{
		// First, search direct path
		List<Vector2Int> path = DirectPath( origin, target, isPositionAccessible );

		// If the direct path not arrives to target, use A*
		if ( path.Count == 0 || !IsTouchingTarget( path[path.Count - 1], target, isPositionAccessible ) )
			path = AstarPath( origin, target, isPositionAccessible, maxCheckedPositions );

		return path;
	}

	/// <summary>
	/// <para>Obtains a path from origin to target reusing the lastPathToTarget if is possible ( if origin and objective exists in path one before the other ).</para>
	/// <para>If is not possible, uses the Pathfinding method to calculate a new path. See specific documentation of Pathfinding for more information.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="lastPathToTarget">Last path to objective calculated. Checked for reuse.</param>
	/// <param name="maxCheckedPositions">Maximum length of the path if it must be recalculated and cannot be direct.</param>
	/// <returns>List of the positions of the best path found. Not includes the origin position.</returns>
	public static List<Vector2Int> PathfindingWithReusing( Vector2Int origin, Vector2Int target, IsPositionAccessible isPositionAccessible, List<Vector2Int> lastPathToTarget, int maxCheckedPositions )
	{
		// TODO
		List<Vector2Int> path = new List<Vector2Int>();

		// If any calculated path
		if ( lastPathToTarget == null || lastPathToTarget.Count == 0 )
		{
			path = Pathfinding( origin, target, isPositionAccessible, maxCheckedPositions );
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
			for ( pos = 0; pos < lastPathToTarget.Count && isPathClear && !pathContainsObjective; pos++ )
			{
				currentposition = lastPathToTarget[pos];

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
					pathContainsObjective = currentposition == target;
				}
			}

			// If the last path can be used
			if ( pathContainsOrigin && pathContainsObjective )
			{
				path = lastPathToTarget.GetRange( 0, pos - 1 );
			}
			// If is impossible to reuse path
			else
			{
				path = Pathfinding( origin, target, isPositionAccessible, maxCheckedPositions );
			}
		}

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

		// TODO : Better control of blocked targets
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
					position += movement;
					path.Add( position );
				}
				else
					targetNotAccessible = true;
			}
		}

		return path;
	}

	/// <summary>
	/// Auxiliar class to calculate costs and link positions of a A* path.
	/// </summary>
	protected class PosInPath
	{
		public Vector2Int Pos { get; }
		public float AccumulatedCost { get; }
		public PosInPath OriginPos { get; }

		public PosInPath( Vector2Int position, float acummulatedCost, PosInPath originPosition )
		{
			Pos = position;
			AccumulatedCost = acummulatedCost;
			OriginPos = originPosition;
		}

		public override bool Equals( object obj )
		{
			return obj is PosInPath && ( (PosInPath)obj ).Pos.Equals( Pos );
		}

		public override int GetHashCode()
		{
			return Pos.GetHashCode();
		}

		public override string ToString()
		{
			return Pos.ToString();
		}
	}

	// TODO : Better control of blocked targets
	/// <summary>
	/// <para>A* pathfinding algorithm. Calculates the shortest path from origin to target if exists.</para>
	/// <para>If the number of checked positions is greater than maxCheckedPositions or the objective is not accessible, returns a path to the position closest to the target.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="maxCheckedPositions">Maximum number of checked positions to reach the objective.</param>
	/// <returns>The best path found from origin to objective.</returns>
	public static List<Vector2Int> AstarPath( Vector2Int origin, Vector2Int target, IsPositionAccessible isPositionAccessible, int maxCheckedPositions )
	{
		Stopwatch chrono = new Stopwatch();
		chrono.Start();

		List<Vector2Int> path = new List<Vector2Int>();
		Dictionary<Vector2Int, PosInPath> checkedPositions = new Dictionary<Vector2Int, PosInPath>();
		PosInPath originPosInPath = new PosInPath( origin, 0, null );
		Dictionary<Vector2Int, PosInPath> remainingPositions = new Dictionary<Vector2Int, PosInPath>
		{
			{ originPosInPath.Pos, originPosInPath }
		};

		bool targetNotAccessible = false;
		bool targetReached = false;

		PosInPath auxPosInPath;
		PosInPath currentPosInPath = null;
		PosInPath closestToObjective = originPosInPath;
		float closestDistanceToObjective = closestToObjective.Pos.Distance( target );
		PosInPath nextPosInPath;
		float distance;
		
		bool isAlreadyInRemaining;
		bool hasBetterAccumulatedCost;		

		// Calcule path
		while ( !targetReached && !targetNotAccessible )
		{
			// Select next position
			currentPosInPath = null;
			foreach ( KeyValuePair<Vector2Int, PosInPath> entry in remainingPositions )
			{
				auxPosInPath = entry.Value;
				if ( currentPosInPath == null ||
					( auxPosInPath.Pos.Distance( target ) + auxPosInPath.AccumulatedCost ) <
					( currentPosInPath.Pos.Distance( target ) + currentPosInPath.AccumulatedCost ) )
				{
					currentPosInPath = auxPosInPath;
				}
			}

			// Remove from remaining and add to checked
			remainingPositions.Remove( currentPosInPath.Pos );
			checkedPositions.Add( currentPosInPath.Pos, currentPosInPath );

			// Check if already touching target
			if ( IsTouchingTarget( currentPosInPath.Pos, target, isPositionAccessible ) )
				targetReached = true;
			else
			{
				// Check around positions
				foreach ( Vector2Int movement in EightDirections2D )
				{
					nextPosInPath = new PosInPath( currentPosInPath.Pos + movement,
						currentPosInPath.AccumulatedCost + movement.magnitude,
						currentPosInPath );

					// If the position hasn't been checked previously and is accessible
					if ( !checkedPositions.ContainsKey( nextPosInPath.Pos ) && IsPossibleDirection( currentPosInPath.Pos, movement, isPositionAccessible ) )
					{
						isAlreadyInRemaining = remainingPositions.TryGetValue( nextPosInPath.Pos, out auxPosInPath );
						hasBetterAccumulatedCost = isAlreadyInRemaining && nextPosInPath.AccumulatedCost < auxPosInPath.AccumulatedCost;

						// If has a better accumulated cost than an existing one in remaining, substitute it
						if ( hasBetterAccumulatedCost )
							remainingPositions.Remove( auxPosInPath.Pos );

						// If is a new position or has a better accumulated cost, add to remaining
						if ( !isAlreadyInRemaining || hasBetterAccumulatedCost )
						{
							remainingPositions.Add( nextPosInPath.Pos, nextPosInPath );

							// Check if is the closest to target
							distance = nextPosInPath.Pos.Distance( target );
							if ( distance <= closestDistanceToObjective )
							{
								closestToObjective = nextPosInPath;
								closestDistanceToObjective = distance;
							}
						}
					}
				}

				// Check if target isn't accesible
				targetNotAccessible = remainingPositions.Count == 0 || checkedPositions.Count > maxCheckedPositions;
			}
		}

		// If is impossible acces to the target use the closest position as desitiny
		if ( targetNotAccessible )
			currentPosInPath = closestToObjective;

		// If the currentPosition ( best last position found ) is closer to target than the origin is a good path
		if ( currentPosInPath.Pos.Distance( target ) < origin.Distance( target ) )
		{
			// Do the reverse path, from the end to the origin position
			while ( currentPosInPath.Pos != origin )
			{
				path.Add( currentPosInPath.Pos );
				currentPosInPath = currentPosInPath.OriginPos;
			}
			path.Reverse();

			UpdateAstarStatistics( chrono.ElapsedMilliseconds, path.Count, checkedPositions.Count );
		}

		chrono.Stop();

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
	public static bool IsTouchingTarget( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible )
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
		if ( useSameDirection && !lastDirection.Equals( Vector2Int.zero ) && IsPossibleDirection( origin, lastDirection, isPositionAccessible ) )
		{
			nextDirection = lastDirection;
		}
		// Search a new random direction different of last
		else
		{
			int lastDirectionIdx = EightDirections2D.IndexOf( lastDirection );
			// If any previous direction is possible, assign one random
			if ( lastDirectionIdx == -1 )
				lastDirectionIdx = randGenerator.Next( 0, EightDirections2D.Count );

			// Check the possible directions incrementing the offset relative to lastDirection
			int idx;
			bool turnRight;
			Vector2Int possibleDirection;
			for ( int offset = 1; offset < EightDirections2D.Count / 2 && nextDirection == Vector2Int.zero; offset++ )
			{
				turnRight = randGenerator.Next( 0, 2 ) == 0;

				idx = LC_Math.Mod( lastDirectionIdx + ( turnRight ? offset : -offset ), EightDirections2D.Count );
				possibleDirection = EightDirections2D[idx];
				if ( IsPossibleDirection( origin, possibleDirection, isPositionAccessible ) )
					nextDirection = possibleDirection;
				else
				{
					idx = LC_Math.Mod( lastDirectionIdx + ( !turnRight ? offset : -offset ), EightDirections2D.Count );
					possibleDirection = EightDirections2D[idx];
					if ( IsPossibleDirection( origin, possibleDirection, isPositionAccessible ) )
						nextDirection = possibleDirection;
				}
			}

			// If any other direction isn't possible, check the opposite of last direction
			if ( nextDirection == Vector2Int.zero )
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

	public static double RandomDouble( System.Random randomGenerator, Vector2 minInclusiveAndMaxExclusive )
	{
		return minInclusiveAndMaxExclusive.x + randomGenerator.NextDouble() * ( minInclusiveAndMaxExclusive.y - minInclusiveAndMaxExclusive.x );
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