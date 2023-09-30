using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.Pages;

/// <summary>
/// A base class for all view models, inheriting from the <see cref="BindableObject"/>.
/// <br/>
/// NOTE: using this class requires your page/view to inherit from <see cref="BaseContentPage" or <see cref="BaseContentView"/>.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject, ILifeCycleView
{
	private readonly List<BaseViewModel> childViewModels = new();
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

	/// <summary>
	/// Use this method when nesting view models. This is the view model equivalent of master/detail pages.
	/// </summary>
	/// <typeparam name="T">The view model type.</typeparam>
	/// <param name="childViewModel">The child view model to add.</param>
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
	/// <param name="childViewModel">The child view model to remove.</param>
	/// <returns>Child view model</returns>
	protected T RemoveChildViewModel<T>(T ChildViewModel) where T : BaseViewModel
	{
		this.childViewModels.Remove(ChildViewModel);
		return ChildViewModel;
	}

	/// <summary>
	/// Called by the parent page when it appears on screen, <em>after</em> the <see cref="Appearing"/> method is called.
	/// </summary>
	public async Task RestoreState()
	{
		foreach (BaseViewModel ChildViewModel in this.childViewModels)
		{
			await ChildViewModel.DoRestoreState();
		}

		await this.DoRestoreState();
	}

	/// <summary>
	/// Called by the parent page when it disappears on screen, <em>before</em> the <see cref="Disappearing"/> method is called.
	/// </summary>
	public async Task SaveState()
	{
		foreach (BaseViewModel ChildViewModel in this.childViewModels)
		{
			await ChildViewModel.DoSaveState();
		}

		await this.DoSaveState();
	}

	/// <summary>
	/// Convenience method that calls <see cref="SaveState"/> and then <see cref="Disappearing"/>.
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
	/// <param name="propertyName">The property name to convert into a settings key.</param>
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
	/// Gets or sets a value which indicates if the protective overlay with a spinner is visible.
	/// </summary>
	public bool IsOverlayVisible
	{
		get => this.isOverlayVisible;
		set
		{
			if (this.isOverlayVisible == value)
			{
				return;
			}

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
					Task.Delay(MinimumOverlayTime - ElapsedTime).GetAwaiter().OnCompleted(() => this.IsOverlayVisible = false);
				}
			}
		}
	}

	/// <summary>
	/// Method called when view is initialized for the first time. Use this method to implement registration
	/// of event handlers, processing navigation arguments, etc.
	/// </summary>
	public Task DoInitialize()
	{
		if (!this.IsInitialized)
		{
			this.IsInitialized = true;

			return this.OnInitialize();
		}

		return Task.CompletedTask;
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
		{
			await this.DoDisappearing();
		}

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
	public async Task DoAppearing()
	{
		if (!this.IsInitialized)
		{
			await this.DoInitialize();
		}

		if (!this.IsAppearing)
		{
			DeviceDisplay.KeepScreenOn = true;

			await this.OnAppearing();

			foreach (BaseViewModel ChildViewModel in this.childViewModels)
			{
				await ChildViewModel.DoAppearing();
			}

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
			{
				await ChildViewModel.DoDisappearing();
			}

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
}
