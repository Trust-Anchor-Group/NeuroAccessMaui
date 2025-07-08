using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Base view model for modal pages.
    /// </summary>
    public class BaseModalViewModel : BaseViewModel
    {
        private readonly TaskCompletionSource popped = new();

        /// <summary>
        /// Task that completes when the modal is popped.
        /// </summary>
        public Task Popped => this.popped.Task;

        /// <summary>
        /// Internal call when the modal is popped.
        /// </summary>
        internal async Task OnPopInternal()
        {
            await this.OnPop();
            this.popped.TrySetResult();
        }

        /// <summary>
        /// Called when the modal is popped.
        /// </summary>
        public virtual Task OnPop()
        {
            return Task.CompletedTask;
        }
    }
}
