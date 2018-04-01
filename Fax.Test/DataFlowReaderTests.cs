using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fax.IO;
using Fax.Reader;
using FluentAssertions;
using NUnit.Framework;

namespace Fax.Test
{
	[TestFixture]
	public class DataFlowReaderTests
	{
		[Test]
		[TestCase( "ааа ббб ааа ввв ббб ввв ббб ббб" )]
		public void DataFlowReaderReadsDataCorrectly( string anyText )
			=> new DataFlowReader( 2 )
			.ReadFlow
			(
				new StreamDataFlowProvider( () => new MemoryStream( Encoding.UTF8.GetBytes( anyText ) ) )
			)
			.ToArray()
			.ShouldHaveMatchingItems
			(
				new[]
				{
					new TableRow( 2, 1.75, 4 ),
					new TableRow( 4, 2.5, 2 ),
					new TableRow( 8, 3.0, 1 )
				}
			);
	}
}
