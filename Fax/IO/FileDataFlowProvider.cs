using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fax.IO
{
	public class FileDataFlowProvider : IDataFlowProvider
	{
		private readonly IDataFlowProvider _innerProvider;

		public FileDataFlowProvider( string file )
			=> _innerProvider
			= new StreamDataFlowProvider( () => File.Open( file, FileMode.Open, FileAccess.Read, FileShare.Read ) );

		public IDataFlow CreateFlow() => _innerProvider.CreateFlow();
	}
}
