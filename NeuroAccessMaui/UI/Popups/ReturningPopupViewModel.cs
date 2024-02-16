using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups
{
	public class ReturningPopupViewModel<TReturn> : BasePopupViewModel
	{
		protected readonly TaskCompletionSource<TReturn?> result = new();

		public Task<TReturn?> Result => this.result.Task;

		public override void OnPop()
		{
			if (!this.Result.IsCompleted)
			{
				this.result.TrySetResult(default(TReturn?));
			}
		}
	}
}
