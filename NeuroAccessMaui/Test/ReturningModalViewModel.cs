using System.Threading.Tasks;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Base view model for modals returning a value.
    /// </summary>
    public class ReturningModalViewModel<TReturn> : BaseModalViewModel
    {
        private readonly TaskCompletionSource<TReturn?> result = new();

        /// <summary>
        /// Task that completes with the modal result.
        /// </summary>
        public Task<TReturn?> Result => this.result.Task;

        /// <summary>
        /// Sets the modal result.
        /// </summary>
        /// <param name="Result">Result value.</param>
        public void SetResult(TReturn? Result)
        {
            this.result.TrySetResult(Result);
        }
    }
}
