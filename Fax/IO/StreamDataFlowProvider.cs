using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fax.IO
{
	public class StreamDataFlowProvider : IDataFlowProvider
	{
		private const int _defaultInitialBufferSize = 0x100;

		private readonly Func<Stream> _streamFactory;
		private readonly int _initialBufferSize;

		public StreamDataFlowProvider( Func<Stream> streamFactory, int initialBufferSize = _defaultInitialBufferSize )
			=> (_streamFactory, _initialBufferSize) = (streamFactory, initialBufferSize);

		public IDataFlow CreateFlow() => new StreamDataFlow( _streamFactory(), _initialBufferSize );

		private class StreamDataFlow : IDataFlow
		{
			private readonly StreamReader _streamReader;
			private readonly int _defaultBufferSize;

			private char[] _buffer;
			private int _readOffset;
			private int _writeOffset;

			public StreamDataFlow( Stream stream, int initialBufferSize )
			{
				///<see cref="StreamReader.BaseStream"/> would be closed after <see cref="_streamReader"/> would be disposed
				_streamReader = new StreamReader( stream );
				_defaultBufferSize = initialBufferSize;
			}

			public void Dispose() => _streamReader.Dispose();

			public IEnumerator<string> GetEnumerator()
			{
				_buffer = new char[ _defaultBufferSize ];

				while ( true )
				{
					int? nextWhitespace = EnsureBufferIsNotEmptyAndReturnNextWhitespaceOffset();

					if ( nextWhitespace.HasValue )
					{
						yield return GetItem( nextWhitespace.Value );
					}
					else
					{
						if ( _readOffset < _writeOffset )
							yield return GetItem( _writeOffset );

						yield break;
					}
				}

				int? EnsureBufferIsNotEmptyAndReturnNextWhitespaceOffset()
				{
					int? nextWhitespaceOffset = TryFindNextWhitespace( _readOffset );

					if ( nextWhitespaceOffset.HasValue )
						return nextWhitespaceOffset;

					if ( _readOffset > 0 )
						ShiftBuffer();

					while ( true )
					{
						while ( _writeOffset < _buffer.Length )
						{
							int writeOffset = _writeOffset;
							int charsRead = _streamReader.Read( _buffer, _writeOffset, _buffer.Length - _writeOffset );

							if ( charsRead <= 0 )
								return default;

							_writeOffset += charsRead;
							int? endlineOffset = TryFindNextWhitespace( writeOffset );

							if ( endlineOffset.HasValue )
								return endlineOffset;
						}

						ResizeBuffer();
					}
				}

				int? TryFindNextWhitespace( int position )
				{
					const char whitespace = ' ';

					for ( int currentOffset = position; currentOffset < _writeOffset; currentOffset++ )
					{
						if ( _buffer[ currentOffset ] == whitespace )
							return currentOffset;
					}

					return default;
				}

				void ShiftBuffer()
				{
					int contentLength = _writeOffset - _readOffset;
					if ( contentLength > 0 )
						Array.Copy( _buffer, _readOffset, _buffer, 0, contentLength );

					_readOffset = 0;
					_writeOffset = contentLength;
				}

				void ResizeBuffer()
				{
					char[] newBuffer = new char[ _buffer.Length * 2 ];
					Array.Copy( _buffer, 0, newBuffer, 0, _buffer.Length );
					_buffer = newBuffer;
				}

				string GetItem( int nextWhitespace )
				{
					string item = new string( _buffer, _readOffset, nextWhitespace - _readOffset );
					_readOffset = nextWhitespace + 1;
					return item;
				}
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
