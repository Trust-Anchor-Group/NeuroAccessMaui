using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Helper class providing consistent animation execution semantics.
	/// </summary>
	public static class AnimationRunner
	{
		/// <summary>
		/// Executes an animation while wiring cancellation tokens to MAUI animation APIs.
		/// </summary>
		/// <param name="Target">Target view.</param>
		/// <param name="Execute">Delegate performing the animation.</param>
		/// <param name="Token">Cancellation token.</param>
		/// <returns>Task tracking execution.</returns>
		public static async Task RunAsync(VisualElement Target, Func<CancellationToken, Task> Execute, CancellationToken Token)
		{
			ArgumentNullException.ThrowIfNull(Target);
			ArgumentNullException.ThrowIfNull(Execute);

			if (!Token.CanBeCanceled)
			{
				await Execute(CancellationToken.None);
				return;
			}

			using CancellationTokenRegistration Registration = Token.Register(() =>
			{
				MainThread.BeginInvokeOnMainThread(() => Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(Target));
			});

			await Execute(Token);
		}
	}
}
