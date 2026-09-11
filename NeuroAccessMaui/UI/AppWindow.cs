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

		/// <summary>
		/// Initializes a new instance of the <see cref="AppWindow"/> class.
		/// </summary>
		/// <param name="app">Application owner.</param>
		/// <param name="rootPage">Root page to host.</param>
		public AppWindow(App app, Page rootPage)
			: base(rootPage)
		{
			this.app = app;
		}

		/// <inheritdoc/>
		protected override void OnStopped()
		{
			base.OnStopped();
			_ = App.ObserveLifecycleAsync(this.app.HandleWindowStoppedAsync);
		}

		/// <inheritdoc/>
		protected override void OnResumed()
		{
			base.OnResumed();
			_ = App.ObserveLifecycleAsync(this.app.HandleWindowResumedAsync);
		}

		/// <inheritdoc/>
		protected override void OnDestroying()
		{
			base.OnDestroying();
			_ = App.ObserveLifecycleAsync(this.app.HandleWindowDestroyingAsync);
		}

	}
}
