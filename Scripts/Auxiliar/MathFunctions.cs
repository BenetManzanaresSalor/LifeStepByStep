using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Math helper class. Includes A* pathfinding.
/// </summary>
public static class MathFunctions
{
	#region Constants

	/// <summary>List with the four basic directions (North, East, South and West)</summary>
	public static readonly List<Vector2Int> FourDirections2D = new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
	/// <summary>List with the eight basic directions (North, NorthEast, East, SouthEast, South, SouthWest, West and NorthWest)</summary>
	public static readonly List<Vector2Int> EightDirections2D = new List<Vector2Int>
		{ Vector2Int.up, Vector2Int.up + Vector2Int.right, Vector2Int.right, Vector2Int.right + Vector2Int.down, Vector2Int.down,
		Vector2Int.down + Vector2Int.left, Vector2Int.left,  Vector2Int.left + Vector2Int.up };

	#endregion

	#region Statistics

	public static int NumCallsAstar { get; private set; }
	public static float AverageTimeAstar { get; private set; }
	public static float MaxTimeAstar { get; private set; }
	public static float AveragePathLengthAstar { get; private set; }
	public static float MaxPathLengthAstar { get; private set; }
	public static float AverageCheckedPositionsAstar { get; private set; }
	public static float MaxCheckedPositionsAstar { get; private set; }

	private static void UpdateAstarStatistics( long time, int pathLength, int checkedPositions )
	{
		if ( time > MaxTimeAstar )
			MaxTimeAstar = time;

		NumCallsAstar++;
		AverageTimeAstar = ( AverageTimeAstar * NumCallsAstar + time ) / NumCallsAstar;

		AveragePathLengthAstar = ( AveragePathLengthAstar * NumCallsAstar + pathLength ) / NumCallsAstar;
		if ( pathLength > MaxPathLengthAstar )
			MaxPathLengthAstar = pathLength;

		AverageCheckedPositionsAstar = ( AverageCheckedPositionsAstar * NumCallsAstar + checkedPositions ) / NumCallsAstar;
		if ( checkedPositions > MaxCheckedPositionsAstar )
			MaxCheckedPositionsAstar = checkedPositions;
	}

	/// <summary>
	/// <para>Reset all the current statistics of MathFunctions.</para>
	/// <para>Includes: A*</para>
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
	/// <para>Includes: A*</para>
	/// </summary>
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
	}

	#endregion

	#region Pathfinding

	/// <summary>
	/// Checks if a position is accessible.
	/// </summary>
	/// <param name="position">Position to check.</param>
	/// <returns>If the position is accessible.</returns>
	public delegate bool IsPositionAccessible( Vector2Int position );

	/// <summary>
	/// <para>Tries to get a path from the origin to the target.</para>
	/// <para>First will search a direct path to target and, if it is not possible, will use the A* algorithm.</para>
	/// <para>If the path to target is impossible, returns a path to the position closest to the objective.</para>
	/// <para>Start position will not be included in result path.</para>
	/// <para>maxPathLenght will be used only if a direct path to target cannot be found.</para>
	/// </summary>
	/// <param name="origin">Start position (will not be included in result path).</param>
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
	/// <para>Calculates the direct path (straight or diagonal line) if is possible or return a incomplete path.</para>
	/// </summary>
	/// <param name="origin">Start position (will not be included in result path).</param>
	/// <param name="objective">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>A direct path to objective if is possible, or an incomplete path instead.</returns>
	public static List<Vector2Int> DirectPath( Vector2Int origin, Vector2Int objective, IsPositionAccessible isPositionAccessible )
	{
		List<Vector2Int> path = new List<Vector2Int>();

		if ( !IsTargetAccessible( objective, isPositionAccessible ) )
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
	/// Checks if the target is accessible by at least one of the surrounding positions ( in eight directions ).
	/// </summary>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>True only if the objective can be accesed by at least one surrounding position.</returns>
	public static bool IsTargetAccessible( Vector2Int target, IsPositionAccessible isPositionAccessible )
	{
		bool isAccessible = false;

		// For all directions, while a acces to the target isn't found
		for ( int i = 0; i < EightDirections2D.Count && !isAccessible; i++ )
			isAccessible = IsTouchingTarget( target + EightDirections2D[i], target, isPositionAccessible );

		return isAccessible;
	}

	/// <summary>
	/// <para>Checks if the target is accesible from the origin position in a single movement (in eight directions)</para>
	/// <para>If is a diagonal movement, checks the accessibility of the left and right positions.</para>
	/// </summary>
	/// <param name="origin">Original position.</param>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <returns>If the objective is accesible from the origin position in a single movement.</returns>
	public static bool IsTouchingTarget( Vector2Int origin, Vector2Int target, IsPositionAccessible isPositionAccessible )
	{
		bool isTouching = false;

		if ( origin.Equals( target ) )
			isTouching = true;
		else
		{
			Vector2Int offset = target - origin;

			if ( Mathf.Abs( offset.x ) <= 1 && Mathf.Abs( offset.y ) <= 1 )
			{
				isTouching = true;

				// If is a diagonal
				if ( offset.x != 0 && offset.y != 0 )
					isTouching = isPositionAccessible( origin + Vector2Int.right * offset.x ) &&
						isPositionAccessible( origin + Vector2Int.up * offset.y );
			}
		}

		return isTouching;
	}

	/// <summary>
	/// <para>Checks if a movement from a origin with a specific direction (in some of the eight directions) is possible.</para>
	/// <para>To check it the method observe the adjacents positions, avoiding a movement that go throught an inaccesible positions.</para>
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

	/// <summary>
	/// <para>A* pathfinding algorithm. Calculates the shortest path from origin to target if exists.</para>
	/// <para>If the number of checked positions is greater than maxCheckedPositions or the objective is not accessible, returns a path to the closest position found.</para>
	/// </summary>
	/// <param name="origin">Start position (will not be included in result path).</param>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="maxCheckedPositions">Maximum number of checked positions to reach the objective.</param>
	/// <returns>The best path found from origin to target.</returns>
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
					( auxPosInPath.AccumulatedCost + auxPosInPath.Pos.Distance( target ) ) <
					( currentPosInPath.AccumulatedCost + currentPosInPath.Pos.Distance( target ) ) )
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
				targetNotAccessible = remainingPositions.Count == 0 || checkedPositions.Count >= maxCheckedPositions;
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
	/// <para>Obtains a path from origin to target reusing the lastPathToTarget if is Possible and Needed.</para>
	/// <para>Possible = The path is clear and arrives to target.</para>
	/// <para>Needed = The distance to target is greater than half of maxCheckedPositions.</para>
	/// <para>Else, uses the Pathfinding method to calculate a new path.</para>
	/// </summary>
	/// <param name="origin">Start position ( will not be included in result path ).</param>
	/// <param name="target">Objective position.</param>
	/// <param name="isPositionAccessible">Method to check if a position is accessible.</param>
	/// <param name="maxCheckedPositions">Maximum length of the path if it must be recalculated and cannot be direct.</param>
	/// <param name="lastPathToTarget">Last path to objective calculated. Checked for reuse.</param>
	/// <returns>List of the positions of the best path found. Not includes the origin position.</returns>
	public static List<Vector2Int> PathfindingWithReusing( Vector2Int origin, Vector2Int target, IsPositionAccessible isPositionAccessible, int maxCheckedPositions, List<Vector2Int> lastPathToTarget )
	{
		List<Vector2Int> path;

		// If any calculated path or the target is close
		if ( lastPathToTarget == null || lastPathToTarget.Count == 0 || origin.Distance( target ) < maxCheckedPositions / 2 )
		{
			path = Pathfinding( origin, target, isPositionAccessible, maxCheckedPositions );
		}
		// Is some path previously calculated
		else
		{
			int idx;
			bool pathIsClear = true;
			bool pathTouchsTarget = false;
			int originIdxInPath = -1;
			Vector2Int pos;
			Vector2Int lastPos = Vector2Int.zero;
			Vector2Int dir;

			// Check is the path is clear and if some position is already done
			for ( idx = 0; idx < lastPathToTarget.Count && pathIsClear && !pathTouchsTarget; idx++ )
			{
				pos = lastPathToTarget[idx];

				// Check if direction is possible
				if ( idx != 0 )
				{
					dir = pos - lastPos;
					pathIsClear = IsPossibleDirection( lastPos, dir, isPositionAccessible );
				}
				lastPos = pos;

				if ( pathIsClear )
					pathTouchsTarget = IsTouchingTarget( pos, target, isPositionAccessible );

				// Check if path contains origin in order to adapt output path
				if ( pos == origin )
				{
					originIdxInPath = idx;
					pathIsClear = idx != lastPathToTarget.Count - 1; // Can't be the last position of the path
				}
			}

			// If the last path can be used
			if ( pathIsClear && pathTouchsTarget )
				path = lastPathToTarget.GetRange( originIdxInPath + 1, idx - 1 );
			else
				path = Pathfinding( origin, target, isPositionAccessible, maxCheckedPositions );
		}

		return path;
	}

	#endregion

	#region Random generation

	/// <summary>
	/// <para>Calculates a pseudo-random direction based on lastDirection and a random value.</para>
	/// <para>If the random value is greater than sameDirectionProbability it will compute a diferent direction.</para>
	/// <para>For that, it will test alternate directions trying to minimize the turn from the lastDirection.</para>
	/// <para>If no direction is possible, returns Vector2Int.zero.</para>
	/// </summary>
	/// <param name="origin">Original position.</param>
	/// <param name="lastDirection">Last direction. Must be a normalized vector.</param>
	/// <param name="randGenerator">Random generator for the direction.</param>
	/// <param name="sameDirectionProbability">Probability of conserve the lastDirection.</param>
	/// <param name="isPositionAccessible">>Method to check if a position is accessible.</param>
	/// <returns>The new direction or, if no one is possible, Vector2Int.zero.</returns>
	public static Vector2Int PseudorandomDirection( Vector2Int origin, Vector2Int lastDirection, System.Random randGenerator, float sameDirectionProbability, IsPositionAccessible isPositionAccessible )
	{
		Vector2Int nextDirection = Vector2Int.zero;
		lastDirection = lastDirection.TransformToDirection();

		float sameDirectionProb = Mathf.Clamp( sameDirectionProbability, 0, 100 );
		bool useSameDirection = RandomDouble( randGenerator, 0, 100 ) <= sameDirectionProb;

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

	/// <summary>
	/// Computes a random double between the minimum value (included) and the maximum value (not included)
	/// </summary>
	public static double RandomDouble( System.Random randomGenerator, double minInclusive, double maxExclusive )
	{
		return minInclusive + randomGenerator.NextDouble() * ( maxExclusive - minInclusive );
	}

	/// <summary>
	/// Computes a random double between the minimum value (first position of the vector, included) and the maximum value (second position of the vector, not included)
	/// </summary>
	public static double RandomDouble( System.Random randomGenerator, Vector2 minInclusiveAndMaxExclusive )
	{
		return RandomDouble( randomGenerator, minInclusiveAndMaxExclusive.x, minInclusiveAndMaxExclusive.y );
	}

	#endregion

	#region Extended methods

	/// <summary>
	/// Computes the euclidean distance from position a to position b.
	/// </summary>
	/// <param name="a">Current position.</param>
	/// <param name="b">Other position.</param>
	public static float Distance( this Vector2Int a, Vector2Int b )
	{
		return ( b - a ).magnitude;
	}

	/// <summary>
	/// Transforms current vector to a discrete direction ( integer values in range [-1, 1] ).
	/// </summary>
	public static Vector2Int TransformToDirection( this Vector2Int vector )
	{
		vector.x = Mathf.Clamp( vector.x, -1, 1 );
		vector.y = Mathf.Clamp( vector.y, -1, 1 );

		return vector;
	}

	/// <summary>
	/// Computes the component-per-component division between the vector a and vector b.
	/// </summary>
	public static Vector3 Div( this Vector3 a, Vector3 b )
	{
		return new Vector3( a.x / b.x, a.y / b.y, a.z / b.z );
	}

	#endregion
}