using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Events;
using System.Reflection;

namespace NeuroAccessMaui.UI.Pages
{
	/// <summary>
	/// A base class for all pages, intended for custom navigation with explicit life-cycle events.
	/// </summary>
	public abstract class BaseContentPage : ContentView, ILifeCycleView
	{
		/// <summary>
		/// Access the page's strongly-typed view model.
		/// </summary>
		public T ViewModel<T>() where T : BaseViewModel
		{
			if (BindingContext is T vm)
				return vm;
			throw new InvalidOperationException($"Expected BindingContext of type {typeof(T).Name}, but got {BindingContext?.GetType().Name ?? "null"}");
		}

		/// <summary>
		/// Convenience property for accessing the BindingContext as a BaseViewModel.
		/// </summary>
		protected BaseViewModel ContentPageModel
		{
			get => this.ViewModel<BaseViewModel>();
			set => this.BindingContext = value;
		}

		/// <summary>
		/// Gets or sets a unique identifier for distinguishing this page from others of the same type.
		/// </summary>
		public virtual string? UniqueId { get; set; }

		/// <summary>
		/// True if this page is initialized (see ILifeCycleView).
		/// </summary>
		public bool IsInitialized { get; private set; }

		/// <summary>
		/// True if this page is currently visible/appearing (see ILifeCycleView).
		/// </summary>
		public bool IsAppearing { get; private set; }

		/// <summary>
		/// Initializes the page with platform safe-area and default background.
		/// </summary>
		protected BaseContentPage()
		{
			//this.On<iOS>().SetUseSafeArea(true);
			this.SetDynamicResource(Microsoft.Maui.Controls.VisualElement.BackgroundColorProperty, "SurfaceBackgroundWL");
			this.Padding = ServiceRef.PlatformSpecific.GetInsets();
		}

		/// <summary>
		/// Override to register event handlers, process navigation args, etc.
		/// Called ONCE per page lifetime.
		/// </summary>
		public virtual async Task OnInitializeAsync()
		{
			// Forward to ViewModel if it implements ILifeCycleView
			if (BindingContext is ILifeCycleView vm)
				await vm.OnInitializeAsync();
		}

		/// <summary>
		/// Override to unregister event handlers, cleanup, etc. Called when page is permanently removed.
		/// </summary>
		public virtual async Task OnDisposeAsync()
		{
				if (BindingContext is ILifeCycleView vm)
					await vm.OnDisposeAsync();
		}

		/// <summary>
		/// Called when the page is about to be shown. Triggers restore state, appearing logic, events, etc.
		/// </summary>
		public virtual async Task OnAppearingAsync()
		{
				try
				{
					if (BindingContext is BaseViewModel vm)
					{
						await vm.OnAppearingAsync();
						if (await ServiceRef.SettingsService.WaitInitDone())
							await vm.RestoreState();
					}

					// Events (optional)
					if (OnBeforeAppearing is not null)
						await OnBeforeAppearing(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
		}

		/// <summary>
		/// Called when the page is about to be hidden. Triggers save state, disappearing logic, events, etc.
		/// </summary>
		public virtual async Task OnDisappearingAsync()
		{

				try
				{
					if (BindingContext is BaseViewModel vm)
					{
						if (await ServiceRef.SettingsService.WaitInitDone())
							await vm.SaveState();

						await vm.OnDisappearingAsync();
					}

					// Events (optional)
					if (OnAfterDisappearing is not null)
						await OnAfterDisappearing(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
		}

		/// <summary>
		/// Event raised before page appears.
		/// </summary>
		public event EventHandlerAsync? OnBeforeAppearing;

		/// <summary>
		/// Event raised after page disappears.
		/// </summary>
		public event EventHandlerAsync? OnAfterDisappearing;

		/// <summary>
		/// Override for handling the custom navigation bar's back button. Return true to cancel the back navigation.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled by the override.</returns>
		public virtual Task<bool> OnToolbarBackButtonPressedAsync()
		{
			return Task.FromResult(false);
		}
	}
}
