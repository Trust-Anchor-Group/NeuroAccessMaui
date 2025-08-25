using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
	public interface IAsyncPolicy
	{
		Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);
	}
}
