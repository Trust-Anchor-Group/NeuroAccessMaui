using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.UI.Pages.Registration;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Events;
using Waher.Networking;

namespace NeuroAccessMaui.UI.Pages
{
	public abstract class BaseContentPage : ContentPage
	{
		/// <summary>
		/// Convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
		/// </summary>
		protected BaseViewModel ContentPageModel
		{
			set => this.BindingContext = value;
			get => this.ViewModel<BaseViewModel>();
		}

		/// <summary>
		/// Convenience function for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
		/// </summary>
		public T ViewModel<T>() where T : BaseViewModel
		{
			if (this.BindingContext is T ViewModel)
				return ViewModel;

			throw new ArgumentException("Wrong view model type: " + typeof(T).FullName);
		}

		/// <summary>
		/// Gets or sets a unique identifier, which allows this page to be distinguished from other pages of the same type without using
		/// references to the page object itself.
		/// </summary>
		public virtual string? UniqueId { get; set; }

		/// <summary>
		/// Creates an instance of the <see cref="BaseContentPage"/> class.
		/// </summary>
		public BaseContentPage()
			: base()
		{
			this.On<iOS>().SetUseSafeArea(true);
			this.SetBackgroundColor();
			Microsoft.Maui.Controls.Application.Current!.RequestedThemeChanged += this.OnRequestedThemeChanged;


			this.Loaded += this.BaseContentPage_Loaded;
		}

		private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
		{
			// Update the background color when the theme changes
			this.SetBackgroundColor();
		}

		private void SetBackgroundColor()
		{
			this.BackgroundColor = AppColors.PrimaryBackground;
		}

		private void BaseContentPage_Loaded(object? sender, EventArgs e)
		{
			try
			{
				this.OnLoaded();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Method called when page has been loaded.
		/// </summary>
		protected virtual void OnLoaded()
		{
		}

		/// <inheritdoc />
		protected sealed override async void OnAppearing()
		{
			try
			{
				base.OnAppearing();
				
				await this.OnAppearingAsync();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected virtual async Task OnAppearingAsync()
		{
			BaseViewModel ViewModel = this.ViewModel<BaseViewModel>();
			bool DoAppearing = !ViewModel.IsAppearing;

			if (DoAppearing)
			{
				EventHandlerAsync? BeforeAppearing = this.OnBeforeAppearing;

				if (BeforeAppearing is not null)
				{
					try
					{
						await BeforeAppearing(this, EventArgs.Empty);
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				}

				try
				{
					await ViewModel.DoAppearing();
				}
				catch (Exception ex)
				{
					ex = Log.UnnestException(ex);
					ServiceRef.LogService.LogException(ex);

					string Message = ServiceRef.Localizer[nameof(AppResources.FailedToBindViewModelForPage),
						ViewModel.GetType().FullName ?? string.Empty, this.GetType().FullName ?? string.Empty];

					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Message + Environment.NewLine + ex.Message);
				}
			}

			try
			{
				if (await ServiceRef.SettingsService.WaitInitDone())
					await ViewModel.RestoreState();
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				ServiceRef.LogService.LogException(ex);

				string Message = ServiceRef.Localizer[nameof(AppResources.FailedToRestoreViewModelStateForPage),
					ViewModel.GetType().FullName ?? string.Empty, this.GetType().FullName ?? string.Empty];

				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Message + Environment.NewLine + ex.Message);
			}

			if (DoAppearing)
			{
				EventHandlerAsync? AfterAppearing = this.OnAfterAppearing;

				if (AfterAppearing is not null)
				{
					try
					{
						await AfterAppearing(this, EventArgs.Empty);
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				}
			}
		}

		/// <summary>
		/// Event raised before page appears
		/// </summary>
		public event EventHandlerAsync? OnBeforeAppearing = null;

		/// <summary>
		/// Event raised after page appears
		/// </summary>
		public event EventHandlerAsync? OnAfterAppearing = null;

		/// <inheritdoc />
		protected sealed override async void OnDisappearing()
		{
			try
			{
				await this.OnDisappearingAsync();
				base.OnDisappearing();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected virtual async Task OnDisappearingAsync()
		{
			BaseViewModel ViewModel = this.ViewModel<BaseViewModel>();

			if (ViewModel.IsAppearing)
			{
				try
				{
					if (await ServiceRef.SettingsService.WaitInitDone())
						await ViewModel.SaveState();
				}
				catch (Exception ex)
				{
					ex = Log.UnnestException(ex);
					ServiceRef.LogService.LogException(ex);

					string msg = ServiceRef.Localizer[nameof(AppResources.FailedToSaveViewModelStateForPage),
						ViewModel.GetType().FullName ?? string.Empty, this.GetType().FullName ?? string.Empty];

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						msg + Environment.NewLine + ex.Message);
				}
			}

			try
			{
				await ViewModel.DoDisappearing();
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				ServiceRef.LogService.LogException(ex);

				string msg = ServiceRef.Localizer[nameof(AppResources.FailedToUnbindViewModelForPage),
					ViewModel.GetType().FullName ?? string.Empty, this.GetType().FullName ?? string.Empty];

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					msg + Environment.NewLine + ex.Message);
			}
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected sealed override bool OnBackButtonPressed()
		{
			try
			{
				BaseViewModel ViewModel = this.ViewModel<BaseViewModel>();

				if (ViewModel is RegistrationViewModel RegistrationViewModel)
				{
					if (RegistrationViewModel.GoToPrevCommand.CanExecute(null))
						RegistrationViewModel.GoToPrevCommand.Execute(null);

					return true;
				}
				else
				{
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						if (ViewModel is not null)
							await ViewModel.GoBack();
						else
							await ServiceRef.UiService.GoBackAsync();
					});

					return true;
				}
			}
			catch
			{
				return base.OnBackButtonPressed();
			}
		}

		/// <summary>
		/// A method which is called when the user presses the back button in the application toolbar at the top of a page.
		/// <para>
		/// If you want to cancel or handle the navigation yourself, you can do so in this method and then return <c>true</c>.
		/// </para>
		/// </summary>
		/// <returns>
		/// Whether or not the back navigation was handled by the override.
		/// </returns>
		public virtual bool OnToolbarBackButtonPressed()
		{
			return this.OnBackButtonPressed();
		}

		/// <summary>
		/// Called when the Page's <see cref="Element.Parent"/> property has changed.
		/// </summary>
		protected sealed override async void OnParentSet()
		{
			try
			{
				base.OnParentSet();

				BaseViewModel ViewModel = this.ViewModel<BaseViewModel>();

				if (this.Parent is null)
				{
					if (ViewModel is ILifeCycleView LifeCycleView)
						await LifeCycleView.DoDispose();
				}
				else
				{
					if (ViewModel is ILifeCycleView LifeCycleView)
						await LifeCycleView.DoInitialize();
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

	}
}
