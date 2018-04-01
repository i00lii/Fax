using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fax.IO;

namespace Fax.Reader
{
	public class DataFlowReader : IDataFlowReader
	{
		private readonly int _initialRangeSize;
		public DataFlowReader( int initialRangeSize ) => _initialRangeSize = initialRangeSize;

		public IEnumerable<TableRow> ReadFlow( IDataFlowProvider flowProvider )
		{
			return ReadFlow().Select( builder => builder.ToTableRow() );

			IReadOnlyCollection<TableRowBuilder> ReadFlow()
			{
				using ( IDataFlow dataFlow = flowProvider.CreateFlow() )
				{
					List<TableRowBuilder> dataRows = Init();

					foreach ( string value in dataFlow )
					{
						for ( int i = 0; i < dataRows.Count; i++ )
						{
							if ( dataRows[ i ].AppendValueAndTryResize( value, out TableRowBuilder newBuilder ) )
							{
								dataRows.Add( newBuilder );
							}
						}
					}

					return dataRows;
				}
			}

			List<TableRowBuilder> Init() => new List<TableRowBuilder>() { new TableRowBuilder( _initialRangeSize ) };
		}

		private class TableRowBuilder
		{
			private readonly int _rangeSize;
			private readonly Flushed _flushed;
			private readonly Pending _pending;

			public TableRowBuilder( int rangeSize )
				: this( rangeSize, Pending.Empty() )
			{
			}

			public TableRowBuilder( int rangeSize, Pending pendingRange )
			{
				_rangeSize = rangeSize;
				_pending = pendingRange;
				_flushed = Flushed.Empty();
			}

			public bool AppendValueAndTryResize( string value, out TableRowBuilder newBuilder )
			{
				newBuilder = default;
				bool isResized = false;
				int itemsTotal = _flushed.Count * _rangeSize + _pending.ItemCount;

				if ( ShouldResize() )
				{
					newBuilder = new TableRowBuilder( _rangeSize * 2, new Pending( _pending.Hashset, _pending.ItemCount ) );
					Flush( reCreateHashset: true );
					isResized = true;
				}
				else if ( ShouldFlush() )
				{
					Flush( reCreateHashset: false );
				}

				_pending.Add( value );
				return isResized;

				bool ShouldResize() => itemsTotal == _rangeSize;
				bool ShouldFlush() => itemsTotal > 0 && itemsTotal % _rangeSize == 0;

				void Flush( bool reCreateHashset )
				{
					_flushed.Add( _pending );
					_pending.Clear( reCreateHashset );
				}
			}

			public TableRow ToTableRow()
			{
				int rangesCount = _flushed.Count + ( _pending.ItemCount > 0 ? 1 : 0 );
				double averageUniqueWordsPerRange = (double) ( _flushed.UniqueWordPerRangeSum + _pending.Hashset.Count ) / rangesCount;
				return new TableRow( _rangeSize, averageUniqueWordsPerRange, rangesCount );
			}
		}

		private class Flushed
		{
			public static Flushed Empty() => new Flushed( 0, 0 );

			public int UniqueWordPerRangeSum { get; private set; }
			public int Count { get; private set; }

			public Flushed( int uniqueWordPerRangeSum, int flushedRangesCount )
			{
				UniqueWordPerRangeSum = uniqueWordPerRangeSum;
				Count = flushedRangesCount;
			}

			public void Add( Pending pending )
			{
				UniqueWordPerRangeSum += pending.Hashset.Count;
				Count++;
			}
		}

		private class Pending
		{
			public static Pending Empty() => new Pending( new HashSet<string>(), 0 );

			public HashSet<string> Hashset { get; private set; }
			public int ItemCount { get; private set; }

			public Pending( HashSet<string> hashset, int itemCount )
			{
				Hashset = hashset;
				ItemCount = itemCount;
			}

			public void Add( string item )
			{
				Hashset.Add( item );
				ItemCount++;
			}

			public void Clear( bool reCreateHashset )
			{
				ItemCount = 0;

				if ( reCreateHashset )
				{
					Hashset = new HashSet<string>();
				}
				else
				{
					Hashset.Clear();
				}
			}
		}
	}
}
