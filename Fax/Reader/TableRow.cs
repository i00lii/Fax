using System;
using System.Collections.Generic;

namespace Fax.Reader
{
	public readonly struct TableRow : IEquatable<TableRow>
	{
		public TableRow( int rangeSize, double averageUniqueWordsPerRange, int rangesCount )
		{
			RangeSize = rangeSize;
			AverageUniqueWordsPerRange = averageUniqueWordsPerRange;
			RangesCount = rangesCount;
		}

		public int RangeSize { get; }
		public double AverageUniqueWordsPerRange { get; }
		public int RangesCount { get; }

		public override int GetHashCode()
		{
			unchecked
			{
				const int prime = -1521134295;
				int hash = 12345701;
				hash = hash * prime + EqualityComparer<int>.Default.GetHashCode( RangeSize );
				hash = hash * prime + EqualityComparer<double>.Default.GetHashCode( AverageUniqueWordsPerRange );
				hash = hash * prime + EqualityComparer<int>.Default.GetHashCode( RangesCount );
				return hash;
			}
		}

		public bool Equals( TableRow other ) => EqualityComparer<int>.Default.Equals( RangeSize, other.RangeSize ) && AverageUniqueWordsPerRange == other.AverageUniqueWordsPerRange && EqualityComparer<int>.Default.Equals( RangesCount, other.RangesCount );
		public override bool Equals( object obj ) => obj is Fax.Reader.TableRow other && Equals( other );

		public static bool operator ==( TableRow x, TableRow y ) => x.Equals( y );
		public static bool operator !=( TableRow x, TableRow y ) => !x.Equals( y );
	}
}
