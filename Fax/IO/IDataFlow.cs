using System;
using System.Collections.Generic;

namespace Fax.IO
{
	public interface IDataFlow : IEnumerable<string>, IDisposable
	{
	}
}
