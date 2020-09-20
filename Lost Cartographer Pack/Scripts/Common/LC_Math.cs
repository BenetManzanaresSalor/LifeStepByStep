using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Math and algorithm helper class for Lost Cartographer Pack.
/// </summary>
public static class LC_Math
{
	#region Perlin noise map generation

	/// <summary>
	/// Generates a random float matrix using perlin noise.
	/// </summary>
	/// <param name="size">Size of the result matrix.</param>
	/// <param name="seed">Seed of the result. Preferaby not integer.</param>
	/// <param name="octaves">Determine the number of details of the noise map. Each new octave adds smaller details and can affect performance.</param>
	/// <param name="persistance">Determine the effect of details at the noise map. Big values makes the terrain heights very random.</param>
	/// <param name="lacunarity">Determine the randomness of the details.</param>
	/// <param name="minAndMaxValues">Minimum (x) and maximum (y) output values. Infinity not allowed.</param>
	/// <param name="xOffset">Rows offset for the random generation.</param>
	/// <param name="yOffset">Columns offset for the random generation..</param>
	/// <returns>A random float matrix with the columns and rows specified.</returns>
	public static float[,] PerlinNoiseMap( Vector2Int size, int seed, int octaves, float persistance, float lacunarity, Vector2 minAndMaxValues,
		float scaleDivisor = 1f, int xOffset = 0, int yOffset = 0, bool useGlobalNormalization = false )
	{
		float[,] map = new float[size.x, size.y];
		float perlinValue = 0;
		float amplitude = 1;
		float frequency;
		float sampleX;
		float sampleY;

		float halfX = size.x / 2f;
		float halfY = size.y / 2f;
		float minPerlinValue = float.MaxValue;
		float maxPerlinValue = float.MinValue;

		if ( scaleDivisor <= 0 )
			scaleDivisor = 0.0001f;

		// Initialize octaves (and optionally min and max perlin value for global normalization)
		System.Random randGen = new System.Random( seed );
		Vector2[] octavesOffsets = new Vector2[octaves];
		if ( useGlobalNormalization )
			maxPerlinValue = 0;

		for ( int i = 0; i < octaves; i++ )
		{
			octavesOffsets[i] = new Vector2( randGen.Next( -100000, 100000 ) + xOffset,
				randGen.Next( -100000, 100000 ) + yOffset );

			if ( useGlobalNormalization )
			{
				maxPerlinValue += amplitude;
				amplitude *= persistance;
			}
		}

		if ( useGlobalNormalization )
		{
			maxPerlinValue /= 1.5f; // Adapt maxPerlinValue (theoric) to more probable values
			minPerlinValue = -maxPerlinValue;
		}

		// Initializate map
		for ( int x = 0; x < map.GetLength( 0 ); x++ )
		{
			for ( int y = 0; y < map.GetLength( 1 ); y++ )
			{
				perlinValue = 0;
				amplitude = 1;
				frequency = 1;

				for ( int oct = 0; oct < octaves; oct++ )
				{
					sampleX = ( x - halfX + octavesOffsets[oct].x ) / scaleDivisor * frequency;
					sampleY = ( y - halfY + octavesOffsets[oct].y ) / scaleDivisor * frequency;
					perlinValue += amplitude * ( Mathf.PerlinNoise( sampleX, sampleY ) * 2 - 1 ); // * 2 - 1 to change range [0, 1] to [-1, 1]

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if ( !useGlobalNormalization )
				{
					minPerlinValue = Min( perlinValue, minPerlinValue );
					maxPerlinValue = Max( perlinValue, maxPerlinValue );
				}

				map[x, y] = perlinValue;
			}
		}

		// Normalize map values in Min-Max range
		for ( int x = 0; x < map.GetLength( 0 ); x++ )
		{
			for ( int y = 0; y < map.GetLength( 1 ); y++ )
			{
				map[x, y] = minAndMaxValues.x + Mathf.InverseLerp( minPerlinValue, maxPerlinValue, map[x, y] ) * ( minAndMaxValues.y - minAndMaxValues.x );
			}
		}

		return map;
	}

	#endregion

	#region Split and Merge

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
	/// <param name="mergeSectors">If is true : Some of the mergeable sectors ( equals, contiguous and have a compatible size ) will be merged. Implies an additional cost but fewer sectors will be generated.</param>
	/// <returns>List of the obtained sectors.</returns>
	public static List<QuadTreeSector> SplitAndMerge<T>( Func<int, int, T> get, Func<T, T, bool> equals, int size, bool mergeSectors )
	{
		return SplitAndMerge( get, equals, new QuadTreeSector( 0, 0, size - 1, size - 1 ), mergeSectors );
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
	public static List<QuadTreeSector> SplitAndMerge<T>( Func<int, int, T> get, Func<T, T, bool> equals, QuadTreeSector sector, bool mergeSectors )
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
			result.AddRange( SplitAndMerge( get, equals, sector, mergeSectors ) );
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

	public static int Mod( int x, int m )
	{
		int r = x % m;
		return r < 0 ? r + m : r;
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

	/// <summary>
	/// Obtains the adjacent positions in a square radius, excluding the center.
	/// </summary>
	/// <param name="center">Center of the square area.</param>
	/// <param name="radius">Radius of square area. Minimum 1.</param>
	/// <returns>List of the positions in the square area.</returns>
	public static List<Vector2Int> AroundPositions( Vector2Int center, int radius )
	{
		List<Vector2Int> positions = new List<Vector2Int>();

		Vector2Int areaTopLeftCorner = center + Vector2Int.one * -1 * radius;
		Vector2Int position;
		for ( int x = 0; x <= radius * 2; x++ )
		{
			for ( int y = 0; y <= radius * 2; y++ )
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

	public static int CoordsToIndex( int f, int c, int nColumns )
	{
		return f * nColumns + c;
	}

	public static void IndexToCoords( int index, int nColumns, out int f, out int c )
	{
		f = index / nColumns;
		c = index % nColumns;
	}

	#endregion

	#region Extended methods

	public static Vector2Int Div( this Vector2Int a, Vector2Int b )
	{
		return new Vector2Int( a.x / b.x, a.y / b.y );
	}

	#endregion
}