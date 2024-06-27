using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups
{
	public class ReturningPopupViewModel<TReturn> : BasePopupViewModel
	{
		protected readonly TaskCompletionSource<TReturn?> result = new();

		public Task<TReturn?> Result => this.result.Task;

	}
}
