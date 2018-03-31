using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fax.IO;
using FluentAssertions;
using NUnit.Framework;

namespace Fax.Test
{
	[TestFixture]
	internal class StreamDataFlowTests
	{
		[Test]
		[TestCase( "text" )]
		[TestCase( "even some more text !" )]
		public void StreamDataFlowReadsDataCorrectly( string anyText )
		{
			StreamDataFlowProvider provider = new StreamDataFlowProvider( () => new MemoryStream( Encoding.UTF8.GetBytes( anyText ) ) );

			using ( IDataFlow flow = provider.CreateFlow() )
			{
				flow
					.ToArray()
					.ShouldHaveMatchingItems( anyText.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ) );
			}
		}
	}
}
