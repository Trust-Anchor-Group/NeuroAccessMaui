using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Base class for popup view models handled by <see cref="PopupService"/>.
	/// </summary>
	public class BasePopupViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<bool> popped = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>
		/// Task completed when the popup has been dismissed.
		/// </summary>
		public Task Popped => this.popped.Task;

		internal async Task NotifyPoppedAsync()
		{
			await this.OnPopAsync();
			this.popped.TrySetResult(true);
		}

		protected virtual Task OnPopAsync()
		{
			return Task.CompletedTask;
		}
	}
}
