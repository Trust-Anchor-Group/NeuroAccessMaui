using System.Globalization;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Pages.Registration;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Events;

namespace NeuroAccessMaui.Pages;

public abstract class BaseContentPage : ContentPage
{
	// Navigation service uses Shell and its routing system, which are only available after the application reached the main page.
	// Before that (during on-boarding and the loading page), we need to use the usual Xamarin Forms navigation.
	//!!! This should be changed!!!
	private bool CanUseNavigationService => App.IsOnboarded;

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
		{
			return ViewModel;
		}

		throw new ArgumentException("Wrong view model type: " + nameof(T));
	}

	/// <summary>
	/// Gets or sets a unique identifier, which allows this page to be distinguished from other pages of the same type without using
	/// references to the page object itself.
	/// </summary>
	public virtual string? UniqueId { get; set; }

	/// <summary>
	/// Creates an instance of the <see cref="BaseContentPage"/> class.
	/// </summary>
	protected internal BaseContentPage()
	{
		this.On<iOS>().SetUseSafeArea(true);
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

		if (!ViewModel.IsAppearing)
		{
			try
			{
				await ViewModel.DoAppearing();
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				ServiceRef.LogService.LogException(e);

				string msg = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.FailedToBindViewModelForPage)],
					ViewModel.GetType().FullName, this.GetType().FullName);

				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					msg + Environment.NewLine + e.Message);
			}
		}

		try
		{
			if (await ServiceRef.SettingsService.WaitInitDone())
			{
				await ViewModel.RestoreState();
			}
		}
		catch (Exception e)
		{
			e = Log.UnnestException(e);
			ServiceRef.LogService.LogException(e);

			string msg = string.Format(CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.FailedToRestoreViewModelStateForPage)],
				ViewModel.GetType().FullName, this.GetType().FullName);

			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				msg + Environment.NewLine + e.Message);
		}
	}

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
				{
					await ViewModel.SaveState();
				}
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				ServiceRef.LogService.LogException(e);

				string msg = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.FailedToSaveViewModelStateForPage)],
					ViewModel.GetType().FullName, this.GetType().FullName);

				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					msg + Environment.NewLine + e.Message);
			}
		}

		try
		{
			await ViewModel.DoDisappearing();
		}
		catch (Exception e)
		{
			e = Log.UnnestException(e);
			ServiceRef.LogService.LogException(e);

			string msg = string.Format(CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.FailedToUnbindViewModelForPage)],
				ViewModel.GetType().FullName, this.GetType().FullName);

			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				msg + Environment.NewLine + e.Message);
		}
	}

	/// <summary>
	/// Overrides the back button behavior to handle navigation internally instead.
	/// </summary>
	/// <returns>Whether or not the back navigation was handled</returns>
	protected sealed override bool OnBackButtonPressed()
	{
		BaseViewModel ViewModel = this.ViewModel<BaseViewModel>();

		if (ViewModel is BaseRegistrationViewModel RegistrationViewModel)
		{
			//!!! This should be changed!!!
			/*
			if (RegistrationViewModel.CanGoBack)
			{
				RegistrationViewModel.GoToPrevCommand.Execute(null);
				return true;
			}
			else
			*/
			{
				return base.OnBackButtonPressed();
			}
		}
		else if (this.CanUseNavigationService)
		{
			ServiceRef.NavigationService.GoBackAsync();
			return true;
		}
		else
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
	/// Called when the <see cref="Xamarin.Forms.Page"/>'s <see cref="Xamarin.Forms.Element.Parent"/> property has changed.
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
				{
					await LifeCycleView.DoDispose();
				}
			}
			else
			{
				if (ViewModel is ILifeCycleView LifeCycleView)
				{
					await LifeCycleView.DoInitialize();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Critical(ex);
		}
	}

}


