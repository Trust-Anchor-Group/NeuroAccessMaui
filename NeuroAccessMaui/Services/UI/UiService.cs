using IdApp.Cv;
using IdApp.Cv.ColorModels;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.UI.Tasks;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Popups;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI
{
	/// <inheritdoc/>
	[Singleton]
	public class UiService : LoadableService, IUiService
	{
		private readonly Dictionary<string, NavigationArgs> navigationArgsMap = [];
		private readonly ConcurrentQueue<UiTask> taskQueue = new();
		private readonly Stack<BasePopupViewModel?> viewModelStack = new();
		private NavigationArgs? latestArguments = null;
		private bool isExecutingUiTasks = false;
		private bool isNavigating = false;

		/// <summary>
		/// Creates a new instance of the <see cref="UiService"/> class.
		/// </summary>
		public UiService()
		{
		}

		#region UI-tasks

		private void AddTask(UiTask Task)
		{
			this.taskQueue.Enqueue(Task);

			if (!this.isExecutingUiTasks)
			{
				this.isExecutingUiTasks = true;

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await this.ProcessAllTasks();
				});
			}
		}

		private async Task ProcessAllTasks()
		{
			try
			{
				do
				{
					if (this.taskQueue.TryDequeue(out UiTask? Task))
						await Task.Execute();
				}
				while (!this.taskQueue.IsEmpty);
			}
			finally
			{
				this.isExecutingUiTasks = false;
			}
		}

		#endregion

		#region DisplayAlert

		/// <inheritdoc/>
		public Task<bool> DisplayAlert(string Title, string Message, string? Accept = null, string? Cancel = null)
		{
			DisplayAlert Task = new(Title, Message, Accept, Cancel);
			this.AddTask(Task);
			return Task.CompletionSource.Task;
		}

		/// <inheritdoc/>
		public Task DisplayException(Exception Exception, string? Title = null)
		{
			Exception = Log.UnnestException(Exception);

			StringBuilder sb = new();

			if (Exception is not null)
			{
				sb.AppendLine(Exception.Message);

				while (Exception.InnerException is not null)
				{
					Exception = Exception.InnerException;
					sb.AppendLine(Exception.Message);
				}
			}
			else
				sb.AppendLine(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)]);

			return this.DisplayAlert(
				Title ?? ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], sb.ToString(),
				ServiceRef.Localizer[nameof(AppResources.Ok)]);
		}

		#endregion

		#region DisplayPrompt

		/// <inheritdoc/>
		public Task<string?> DisplayPrompt(string Title, string Message, string? Accept = null, string? Cancel = null)
		{
			DisplayPrompt Task = new(Title, Message, Accept, Cancel);
			this.AddTask(Task);
			return Task.CompletionSource.Task;
		}

		#endregion

		#region Screen shots

		/// <summary>
		/// Takes a blurred screen shot
		/// </summary>
		/// <returns>Screenshot, if able to create a screen shot.</returns>
		public async Task<ImageSource?> TakeBlurredScreenshotAsync()
		{
			try
			{

				IScreenshotResult? Result = await Screenshot.CaptureAsync();
				if (Result is null)
					return null;

				//Read screenshot
				using Stream PngStream = await Result.OpenReadAsync(ScreenshotFormat.Png, 20);

				// Original SKBitmap from PNG stream
				SKBitmap OriginalBitmap = SKBitmap.FromImage(SKImage.FromEncodedData(PngStream));

				// Desired width and height for the downscaled image
				int DesiredWidth = OriginalBitmap.Width / 4;   //Reduce the width by a quarter
				int DesiredHeight = OriginalBitmap.Height / 4; //Reduce the height by a quarter

				// Create an SKImageInfo with the desired width, height, and color type of the original
				SKImageInfo resizedInfo = new(DesiredWidth, DesiredHeight, SKColorType.Gray8);

				// Create a new SKBitmap for the downscaled image
				SKBitmap resizedBitmap = OriginalBitmap.Resize(resizedInfo, SKFilterQuality.Medium);

				//Blur image
				IMatrix RezisedMatrix = Bitmaps.FromBitmap(resizedBitmap);
				IMatrix GreyChannelMatrix = RezisedMatrix.GrayScale();
				IMatrix NewMatrix = IdApp.Cv.Transformations.Convolutions.ConvolutionOperations.GaussianBlur(GreyChannelMatrix, 12, 3.5f);

				// Continue with the blurring and encoding to PNG as before
				byte[] Blurred = Bitmaps.EncodeAsPng(NewMatrix);
				return ImageSource.FromStream(() => new MemoryStream(Blurred));
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
				return null;
			}
		}

		/// <summary>
		/// Takes a screen-shot.
		/// </summary>
		/// <returns>Screen shot, if able.</returns>
		public async Task<ImageSource?> TakeScreenshotAsync()
		{
			try
			{
				IScreenshotResult? Result = await Screenshot.CaptureAsync();
				if (Result is null)
					return null;

				// Read the stream into a memory stream or byte array
				using Stream Stream = await Result.OpenReadAsync();
				MemoryStream MemoryStream = new();
				await Stream.CopyToAsync(MemoryStream);
				byte[] Bytes = MemoryStream.ToArray();

				// Return a new MemoryStream based on the byte array for each invocation
				return ImageSource.FromStream(() => new MemoryStream(Bytes));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return null;
			}
		}

		#endregion

		#region Navigation

		/// <inheritdoc/>
		public Page CurrentPage => Shell.Current.CurrentPage;

		/// <inheritdoc/>
		public override Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				try
				{
					Application? Application = Application.Current;

					if (Application is not null)
					{
						Application.PropertyChanging += this.OnApplicationPropertyChanging;
						Application.PropertyChanged += this.OnApplicationPropertyChanged;
					}

					this.SubscribeToShellNavigatingIfNecessary(Application);

					this.EndLoad(true);
				}
				catch (Exception e)
				{
					ServiceRef.LogService.LogException(e);
					this.EndLoad(false);
				}
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				try
				{
					Application? Application = Application.Current;
					if (Application is not null)
					{
						this.UnsubscribeFromShellNavigatingIfNecessary(Application);
						Application.PropertyChanged -= this.OnApplicationPropertyChanged;
						Application.PropertyChanging -= this.OnApplicationPropertyChanging;
					}
				}
				catch (Exception e)
				{
					ServiceRef.LogService.LogException(e);
				}

				this.EndUnload();
			}

			return Task.CompletedTask;
		}


		/// <inheritdoc/>
		public Task GoToAsync(string Route, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null)
		{
			// No args navigation will create a default navigation arguments
			return this.GoToAsync<NavigationArgs>(Route, null, BackMethod, UniqueId);
		}

		/// <inheritdoc/>
		public async Task GoToAsync<TArgs>(string Route, TArgs? Args, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null) where TArgs : NavigationArgs, new()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				ServiceRef.PlatformSpecific.HideKeyboard();

				// Get the parent's navigation arguments
				NavigationArgs? ParentArgs = this.GetCurrentNavigationArgs();

				// Create a default navigation arguments if Args are null
				NavigationArgs NavigationArgs = Args ?? new();

				NavigationArgs.SetBackArguments(ParentArgs, BackMethod, UniqueId);
				this.PushArgs(Route, NavigationArgs);

				if (!string.IsNullOrEmpty(UniqueId))
					Route += "?UniqueId=" + UniqueId;

				try
				{
					this.isNavigating = true;
					await Shell.Current.GoToAsync(Route, NavigationArgs.Animated);
				}
				catch (Exception e)
				{
					e = Log.UnnestException(e);
					ServiceRef.LogService.LogException(e);
					string ExtraInfo = Environment.NewLine + e.Message;

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.FailedToNavigateToPage), Route, ExtraInfo]);
				}
				finally
				{
					this.isNavigating = false;
				}
			});
		}

		/// <inheritdoc/>
		public async Task GoBackAsync(bool Animate = true)
		{
			try
			{
				NavigationArgs? NavigationArgs = this.GetCurrentNavigationArgs();

				if (NavigationArgs is not null) // the main view?
				{
					string BackRoute = NavigationArgs.GetBackRoute();

					this.isNavigating = true;
					await Shell.Current.GoToAsync(BackRoute, Animate);
				}
				else
				{
					ShellNavigationState State = Shell.Current.CurrentState;
					if (Uri.TryCreate(State.Location, "..", out Uri? BackLocation))
						await Shell.Current.GoToAsync(BackLocation);
					else
						await Shell.Current.GoToAsync(Constants.Pages.MainPage);
				}
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToClosePage)]);
			}
			finally
			{
				this.isNavigating = false;
			}
		}

		/// <summary>
		/// Pops the latests navigation arguments. Can only be used once to get the navigation arguments. Called by constructors to find
		/// associated navigation arguments for a page being constructed.
		/// </summary>
		/// <returns>Latest navigation arguments, or null if not found.</returns>
		public TArgs? PopLatestArgs<TArgs>()
			where TArgs : NavigationArgs, new()
		{
			if (this.latestArguments is TArgs Result)
			{
				this.latestArguments = null;
				return Result;
			}
			else
				return null;
		}

		/// <summary>
		/// Returns the page's arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <param name="UniqueId">View's unique ID.</param>
		/// <returns>View's navigation arguments, or null if not found.</returns>
		public TArgs? TryGetArgs<TArgs>(string? UniqueId = null)
			where TArgs : NavigationArgs, new()
		{
			if (this.TryGetArgs(out TArgs? Result, UniqueId))
				return Result;
			else
				return null;
		}

		/// <summary>
		/// Returns the page's arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <param name="Args">View's navigation arguments.</param>
		/// <param name="UniqueId">View's unique ID.</param>
		public bool TryGetArgs<TArgs>([NotNullWhen(true)] out TArgs? Args, string? UniqueId = null)
			where TArgs : NavigationArgs, new()
		{
			NavigationArgs? NavigationArgs = null;

			if (this.CurrentPage is Page Page)
			{
				NavigationArgs = this.TryGetArgs(Page.GetType().Name, UniqueId);
				string Route = Routing.GetRoute(Page);
				NavigationArgs ??= this.TryGetArgs(Route, UniqueId);

				if ((NavigationArgs is null) && (UniqueId is null) &&
					(Page is BaseContentPage BasePage) && (BasePage.UniqueId is not null))
				{
					return this.TryGetArgs(out Args, BasePage.UniqueId);
				}
			}

			if (NavigationArgs is TArgs TArgsArgs)
				Args = TArgsArgs;
			else
				Args = null;

			return (Args is not null);
		}

		private NavigationArgs? GetCurrentNavigationArgs()
		{
			this.TryGetArgs(out NavigationArgs? Args);
			return Args;
		}

		private void OnApplicationPropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs Args)
		{
			if (Args.PropertyName == nameof(Application.MainPage))
				this.SubscribeToShellNavigatingIfNecessary((Application?)Sender);
		}

		private void OnApplicationPropertyChanging(object? Sender, PropertyChangingEventArgs Args)
		{
			if (Args.PropertyName == nameof(Application.MainPage))
				this.UnsubscribeFromShellNavigatingIfNecessary((Application?)Sender);
		}

		private void SubscribeToShellNavigatingIfNecessary(Application? Application)
		{
			if (Application?.MainPage is Shell Shell)
				Shell.Navigating += this.Shell_Navigating;
		}

		private void UnsubscribeFromShellNavigatingIfNecessary(Application? Application)
		{
			if (Application?.MainPage is Shell Shell)
				Shell.Navigating -= this.Shell_Navigating;
		}

		private void Shell_Navigating(object? Sender, ShellNavigatingEventArgs e)
		{
			try
			{
				if ((e.Source == ShellNavigationSource.Pop) && e.CanCancel && !this.isNavigating)
				{
					e.Cancel();

					MainThread.BeginInvokeOnMainThread(async () =>
					{
						await this.GoBackAsync();
					});
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private static bool TryGetPageName(string Route, [NotNullWhen(true)] out string? PageName)
		{
			PageName = null;

			if (!string.IsNullOrWhiteSpace(Route))
			{
				PageName = Route.TrimStart('.', '/');
				return !string.IsNullOrWhiteSpace(PageName);
			}

			return false;
		}

		private void PushArgs(string Route, NavigationArgs Args)
		{
			this.latestArguments = Args;

			if (TryGetPageName(Route, out string? PageName))
			{
				if (Args is not null)
				{
					string? UniqueId = Args.GetUniqueId();

					if (!string.IsNullOrEmpty(UniqueId))
						PageName += UniqueId;

					this.navigationArgsMap[PageName] = Args;
				}
				else
					this.navigationArgsMap.Remove(PageName);
			}
		}

		private NavigationArgs? TryGetArgs(string Route, string? UniqueId)
		{
			if (!string.IsNullOrEmpty(UniqueId))
				Route += UniqueId;

			if (TryGetPageName(Route, out string? PageName) &&
				this.navigationArgsMap.TryGetValue(PageName, out NavigationArgs? Args))
			{
				return Args;
			}

			return null;
		}

		#endregion

		#region Popups

		/// <inheritdoc/>
		public Task PushAsync<TPage, TViewModel>() where TPage : BasePopup, new() where TViewModel : BasePopupViewModel, new()
		{
			TPage page = new();
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushAsync<TPage, TViewModel>(TPage page, TViewModel viewModel) where TPage : BasePopup where TViewModel : BasePopupViewModel
		{
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushAsync<TPage, TViewModel>(TPage page) where TPage : BasePopup where TViewModel : BasePopupViewModel, new()
		{
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushAsync<TPage, TViewModel>(TViewModel viewModel) where TPage : BasePopup, new() where TViewModel : BasePopupViewModel
		{
			TPage page = new()
			{
				ViewModel = viewModel
			};

			this.viewModelStack.Push(viewModel);

			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushAsync<TPage>() where TPage : BasePopup, new()
		{
			TPage page = new();
			BasePopupViewModel? viewModel = page.ViewModel;
			this.viewModelStack.Push(viewModel ?? null);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushAsync<TPage>(TPage page) where TPage : BasePopup
		{
			BasePopupViewModel? viewModel = page.ViewModel;
			this.viewModelStack.Push(viewModel ?? null);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>() where TPage : BasePopup, new() where TViewModel : ReturningPopupViewModel<TReturn>, new()
		{
			TPage page = new();
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TPage page, TViewModel viewModel) where TPage : BasePopup where TViewModel : ReturningPopupViewModel<TReturn>
		{
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TPage page) where TPage : BasePopup
			where TViewModel : ReturningPopupViewModel<TReturn>, new()
		{
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TViewModel viewModel) where TPage : BasePopup, new() where TViewModel : ReturningPopupViewModel<TReturn>
		{
			TPage page = new()
			{
				ViewModel = viewModel
			};

			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);

			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task PopAsync()
		{
			if (this.viewModelStack.Count == 0)
				return;

			BasePopupViewModel? vm = this.viewModelStack.Pop();
			vm?.OnPop();
			await MopupService.Instance.PopAsync();
		}

		#endregion
	}
}
