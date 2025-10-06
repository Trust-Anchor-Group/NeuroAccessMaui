using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups
{
	public class ReturningPopupViewModel<TReturn> : BasePopupViewModel
	{
		private readonly TaskCompletionSource<TReturn?> result = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task<TReturn?> Result => this.result.Task;

		protected bool TrySetResult(TReturn? value) => this.result.TrySetResult(value);

		protected override Task OnPopAsync()
		{
			if (!this.result.Task.IsCompleted)
			{
				this.result.TrySetResult(default);
			}
			return base.OnPopAsync();
		}
	}
}
