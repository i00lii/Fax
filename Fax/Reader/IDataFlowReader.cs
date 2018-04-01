using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fax.IO;

namespace Fax.Reader
{
	public interface IDataFlowReader
	{
		IEnumerable<TableRow> ReadFlow( IDataFlowProvider flowProvider );
	}
}
