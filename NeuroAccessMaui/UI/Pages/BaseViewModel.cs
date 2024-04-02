﻿using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages
{
	/// <summary>
	/// A base class for all view models, inheriting from the <see cref="BindableObject"/>.
	/// <br/>
	/// NOTE: using this class requires your page/view to inherit from <see cref="BaseContentPage"/> or <see cref="BaseContentView"/>.
	/// </summary>
	public abstract partial class BaseViewModel : ObservableObject, ILifeCycleView
	{
		private readonly List<BaseViewModel> childViewModels = [];
		private bool isOverlayVisible;
		private DateTime overlayLastActivationTime;

		/// <summary>
		/// Create an instance of a <see cref="BaseViewModel"/>.
		/// </summary>
		public BaseViewModel()
		{
		}

		/// <summary>
		/// Returns <c>true</c> if the view model is initialized.
		/// </summary>
		public bool IsInitialized { get; private set; }

		/// <summary>
		/// Returns <c>true</c> if the view model is shown.
		/// </summary>
		public bool IsAppearing { get; private set; }

		/// <summary>
		/// Gets the child view models.
		/// </summary>
		public IEnumerable<BaseViewModel> Children => this.childViewModels;

		public virtual double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);
		public virtual double MaximumViewHeightRequest => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

		/// <summary>
		/// Set a new registration step
		/// </summary>
		public static void GoToRegistrationStep(RegistrationStep NewStep)
		{
			ServiceRef.TagProfile.GoToStep(NewStep);
			WeakReferenceMessenger.Default.Send(new RegistrationPageMessage(ServiceRef.TagProfile.Step));
		}

		/// <summary>
		/// Use this method when nesting view models. This is the view model equivalent of master/detail pages.
		/// </summary>
		/// <typeparam name="T">The view model type.</typeparam>
		/// <param name="ChildViewModel">The child view model to add.</param>
		/// <returns>Child view model</returns>
		protected T AddChildViewModel<T>(T ChildViewModel) where T : BaseViewModel
		{
			this.childViewModels.Add(ChildViewModel);
			return ChildViewModel;
		}

		/// <summary>
		/// Use this method when nesting view models. This is the view model equivalent of master/detail pages.
		/// </summary>
		/// <typeparam name="T">The view model type.</typeparam>
		/// <param name="ChildViewModel">The child view model to remove.</param>
		/// <returns>Child view model</returns>
		protected T RemoveChildViewModel<T>(T ChildViewModel) where T : BaseViewModel
		{
			this.childViewModels.Remove(ChildViewModel);
			return ChildViewModel;
		}

		/// <summary>
		/// Called by the parent page when it appears on screen, <em>after</em> the <see cref="DoAppearing"/> method is called.
		/// </summary>
		public async Task RestoreState()
		{
			foreach (BaseViewModel ChildViewModel in this.childViewModels)
				await ChildViewModel.DoRestoreState();

			await this.DoRestoreState();
		}

		/// <summary>
		/// Called by the parent page when it disappears on screen, <em>before</em> the <see cref="DoDisappearing"/> method is called.
		/// </summary>
		public async Task SaveState()
		{
			foreach (BaseViewModel ChildViewModel in this.childViewModels)
				await ChildViewModel.DoSaveState();

			await this.DoSaveState();
		}

		/// <summary>
		/// Convenience method that calls <see cref="SaveState"/> and then <see cref="DoDisappearing"/>.
		/// </summary>
		public async Task Shutdown()
		{
			await this.SaveState();
			await this.DoDisappearing();
		}

		/// <summary>
		/// Override this method to do view model specific restoring of state when it's parent page/view appears on screen.
		/// </summary>
		protected virtual Task DoRestoreState()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this method to do view model specific saving of state when it's parent page/view disappears from screen.
		/// </summary>
		protected virtual Task DoSaveState()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Helper method for getting a unique settings key for a given property.
		/// </summary>
		/// <param name="PropertyName">The property name to convert into a settings key.</param>
		/// <returns>Key name</returns>
		protected string GetSettingsKey(string PropertyName)
		{
			return this.GetType().FullName + "." + PropertyName;
		}

		/// <summary>
		/// A helper property to set/get when the ViewModel is busy doing work.
		/// </summary>
		[ObservableProperty]
		private bool isBusy;

		/// <summary>
		/// Sets the <see cref="IsBusy"/> property.
		/// </summary>
		/// <param name="IsBusy">New value.</param>
		public virtual void SetIsBusy(bool IsBusy)
		{
			this.IsBusy = IsBusy;
		}

		/// <summary>
		/// Gets or sets a value which indicates if the protective overlay with a spinner is visible.
		/// </summary>
		public bool IsOverlayVisible
		{
			get => this.isOverlayVisible;
			set
			{
				if (this.isOverlayVisible == value)
					return;

				if (value)
				{
					this.isOverlayVisible = true;
					this.overlayLastActivationTime = DateTime.Now;
					this.OnPropertyChanged();
				}
				else
				{
					TimeSpan MinimumOverlayTime = TimeSpan.FromMilliseconds(500);
					TimeSpan ElapsedTime = DateTime.Now.Subtract(this.overlayLastActivationTime);

					if (ElapsedTime >= MinimumOverlayTime)
					{
						this.isOverlayVisible = false;
						this.OnPropertyChanged();
					}
					else
					{
						// It is important to use the property here, not the field, because last activation time might be updated while we are waiting,
						// we need to recheck it and possibly reschedule.
						Task.Delay(MinimumOverlayTime - ElapsedTime).GetAwaiter().OnCompleted(() => this.isOverlayVisible = false);
					}
				}
			}
		}

		/// <summary>
		/// Method called when view is initialized for the first time. Use this method to implement registration
		/// of event handlers, processing navigation arguments, etc.
		/// </summary>
		public async Task DoInitialize()
		{
			if (!this.IsInitialized)
			{
				this.IsInitialized = true;

				await this.OnInitialize();
			}
		}

		/// <summary>
		/// Method called when view is initialized for the first time. Use this method to implement registration
		/// of event handlers, processing navigation arguments, etc.
		/// </summary>
		protected virtual Task OnInitialize()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Method called when the view is disposed, and will not be used more. Use this method to unregister
		/// event handlers, etc.
		/// </summary>
		public async Task DoDispose()
		{
			if (this.IsAppearing)
				await this.DoDisappearing();

			if (this.IsInitialized)
			{
				this.IsInitialized = false;

				await this.OnDispose();
			}
		}

		/// <summary>
		/// Method called when the view is disposed, and will not be used more. Use this method to unregister
		/// event handlers, etc.
		/// </summary>
		protected virtual Task OnDispose()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Method called when view is appearing on the screen.
		/// </summary>
		public virtual async Task DoAppearing()
		{
			if (!this.IsInitialized)
				await this.DoInitialize();

			if (!this.IsAppearing)
			{
				DeviceDisplay.KeepScreenOn = true;

				await this.OnAppearing();

				foreach (BaseViewModel ChildViewModel in this.childViewModels)
					await ChildViewModel.DoAppearing();

				this.IsAppearing = true;
			}
		}

		/// <summary>
		/// Method called when view is appearing on the screen.
		/// </summary>
		protected virtual Task OnAppearing()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Method called when view is disappearing from the screen.
		/// </summary>
		public async Task DoDisappearing()
		{
			if (this.IsAppearing)
			{
				foreach (BaseViewModel ChildViewModel in this.childViewModels)
					await ChildViewModel.DoDisappearing();

				await this.OnDisappearing();

				this.IsAppearing = false;
			}
		}

		/// <summary>
		/// Method called when view is disappearing from the screen.
		/// </summary>
		protected virtual Task OnDisappearing()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Asks the user to confirm an action.
		/// </summary>
		/// <param name="Message">Message to display to the user.</param>
		/// <returns>If the user confirms the action.</returns>
		public static async Task<bool> AreYouSure(string Message)
		{
			return await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Confirm)], Message,
				ServiceRef.Localizer[nameof(AppResources.Yes)],
				ServiceRef.Localizer[nameof(AppResources.No)]);
		}

		/// <summary>
		/// Method called when user wants to navigate to the previous screen.
		/// </summary>
		[RelayCommand]
		public virtual async Task GoBack()
		{
			await ServiceRef.UiService.GoBackAsync();
		}

		/// <summary>
		/// Gets the value of a property in the view model.
		/// </summary>
		/// <param name="PropertyName">Name of property.</param>
		/// <returns>Property value.</returns>
		public virtual object? GetValue(string PropertyName)
		{
			PropertyInfo? PI = this.GetType().GetProperty(PropertyName)
				?? throw new ArgumentException("Property not found: " + PropertyName, nameof(PropertyName));

			return PI.GetValue(this);
		}

		/// <summary>
		/// Sets the value of a property in the view model.
		/// </summary>
		/// <param name="PropertyName">Name of property.</param>
		/// <returns>Property value, if available, null otherwise.</returns>
		public virtual void SetValue(string PropertyName, object? Value)
		{
			PropertyInfo? PI = this.GetType().GetProperty(PropertyName)
				?? throw new ArgumentException("Property not found: " + PropertyName, nameof(PropertyName));

			PI.SetValue(this, Value);
		}

	}
}
