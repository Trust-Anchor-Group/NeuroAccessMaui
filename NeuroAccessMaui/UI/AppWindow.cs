using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Custom window that forwards MAUI lifecycle events to the <see cref="App"/> instance.
	/// </summary>
	internal sealed class AppWindow : Window
	{
		private readonly App app;
		private readonly IActivationState? activationState;

		/// <summary>
		/// Initializes a new instance of the <see cref="AppWindow"/> class.
		/// </summary>
		/// <param name="app">Application owner.</param>
		/// <param name="rootPage">Root page to host.</param>
		/// <param name="activationState">Activation state provided by MAUI.</param>
		public AppWindow(App app, Page rootPage, IActivationState? activationState)
			: base(rootPage)
		{
			this.app = app;
			this.activationState = activationState;
		}

		/// <inheritdoc/>
		protected override void OnCreated()
		{
			base.OnCreated();
			this.app.HandleWindowCreated(this.activationState);
		}

		/// <inheritdoc/>
		protected override void OnStopped()
		{
			base.OnStopped();
			this.FireAndForget(this.app.HandleWindowStoppedAsync);
		}

		/// <inheritdoc/>
		protected override void OnResumed()
		{
			base.OnResumed();
			this.FireAndForget(this.app.HandleWindowResumedAsync);
		}

		/// <inheritdoc/>
		protected override void OnDestroying()
		{
			base.OnDestroying();
			this.FireAndForget(this.app.HandleWindowDestroyingAsync);
		}

		private void FireAndForget(Func<Task> lifecycleHandler)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await lifecycleHandler().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					this.ReportLifecycleException(ex);
				}
			});
		}

		private void ReportLifecycleException(Exception ex)
		{
			try
			{
				ServiceRef.LogService.LogException(ex);
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine(ex);
			}
		}
	}
}
