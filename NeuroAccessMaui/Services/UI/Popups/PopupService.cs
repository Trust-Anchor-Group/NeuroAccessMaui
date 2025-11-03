using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Test;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI.Popups
{
	/// <inheritdoc/>
	[Singleton]
	public class PopupService : IPopupService
	{
		private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
		private readonly Stack<PopupSession> popupStack = new();
		private bool isExecutingQueue;
		private IShellPresenter? attachedPresenter;

		/// <inheritdoc/>
		public event EventHandler? PopupStackChanged;

		/// <inheritdoc/>
		public bool HasOpenPopups => this.popupStack.Count > 0;

		/// <summary>
		/// Gets true if the top popup is blocking.
		/// </summary>
		public bool TopIsBlocking => this.popupStack.Count > 0 && this.popupStack.Peek().Options.IsBlocking;

		/// <summary>
		/// Returns true if any blocking popup exists in the stack.
		/// </summary>
		public bool ContainsBlockingPopup
		{
			get
			{
				foreach (PopupSession Session in this.popupStack)
					if (Session.Options.IsBlocking)
						return true;
				return false;
			}
		}

		/// <inheritdoc/>
		public async Task PushAsync<TPopup>(PopupOptions? Options = null) where TPopup : ContentView
		{
			PopupOptions effectiveOptions = Options ?? new PopupOptions();
			PopupSession session = await this.Enqueue(async () =>
			{
				TPopup popupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				return await this.PrepareAndShowPopupAsync(popupView, popupView.BindingContext, effectiveOptions);
			});
			await session.Completion.Task;
		}

		/// <inheritdoc/>
		public async Task PushAsync<TPopup, TViewModel>(PopupOptions? Options = null)
			where TPopup : ContentView
			where TViewModel : class
		{
			PopupOptions effectiveOptions = Options ?? new PopupOptions();
			PopupSession session = await this.Enqueue(async () =>
			{
				TPopup popupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				TViewModel viewModel = ServiceRef.Provider.GetRequiredService<TViewModel>();
				popupView.BindingContext = viewModel;
				return await this.PrepareAndShowPopupAsync(popupView, viewModel, effectiveOptions);
			});
			await session.Completion.Task;
		}

		public async Task PushAsync<TPopup, TViewModel>(TViewModel ViewModel, PopupOptions? Options = null)
		where TPopup : ContentView
		where TViewModel : class
		{
			PopupOptions effectiveOptions = Options ?? new PopupOptions();
			PopupSession session = await this.Enqueue(async () =>
			{
				TPopup popupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				popupView.BindingContext = ViewModel;
				return await this.PrepareAndShowPopupAsync(popupView, ViewModel, effectiveOptions);
			});
			await session.Completion.Task;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushAsync<TPopup, TViewModel, TReturn>(PopupOptions? Options = null)
			where TPopup : ContentView
			where TViewModel : ReturningPopupViewModel<TReturn>
		{
			PopupOptions effectiveOptions = Options ?? new PopupOptions();
			TViewModel viewModel = ServiceRef.Provider.GetRequiredService<TViewModel>();
			Task<TReturn?> resultTask = viewModel.Result;
			await this.Enqueue(async () =>
			{
				TPopup popupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				popupView.BindingContext = viewModel;
				await this.PrepareAndShowPopupAsync(popupView, viewModel, effectiveOptions);
			});
			return await resultTask;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushAsync<TPopup, TViewModel, TReturn>(TViewModel ViewModel, PopupOptions? Options = null)
			where TPopup : ContentView
			where TViewModel : ReturningPopupViewModel<TReturn>
		{
			if (ViewModel is null)
				throw new ArgumentNullException(nameof(ViewModel));
			PopupOptions effectiveOptions = Options ?? new PopupOptions();
			Task<TReturn?> resultTask = ViewModel.Result;
			await this.Enqueue(async () =>
			{
				TPopup popupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				popupView.BindingContext = ViewModel;
				await this.PrepareAndShowPopupAsync(popupView, ViewModel, effectiveOptions);
			});
			return await resultTask;
		}

		/// <inheritdoc/>
		public async Task PushAsync(ContentView Popup, PopupOptions? Options = null)
		{
			if (Popup is null)
				throw new ArgumentNullException(nameof(Popup));
			PopupOptions effectiveOptions = Options ?? new PopupOptions();
			PopupSession session = await this.Enqueue(async () => await this.PrepareAndShowPopupAsync(Popup, Popup.BindingContext, effectiveOptions));
			await session.Completion.Task;
		}

		/// <inheritdoc/>
		public Task PopAsync()
		{
			return this.Enqueue(this.PopTopAsync);
		}

		private async Task<PopupSession> PrepareAndShowPopupAsync(ContentView popupView, object? viewModel, PopupOptions options)
		{
			options.Normalize();
			await EnsureInitializedAsync(popupView);
			await EnsureInitializedAsync(viewModel);

			if (options.IsBlocking)
			{
				// Remove all non-blocking popups. If a blocking popup already exists, remove it to enforce single modal semantics.
				while (this.popupStack.Count > 0)
					await this.PopTopAsync();
			}

			PopupSession session = new PopupSession(popupView, viewModel, options);
			ApplyOptionsToPopup(popupView, options);
			this.popupStack.Push(session);

			IShellPresenter presenter = this.GetPresenter();
			await presenter.ShowPopup(popupView, options.ShowTransition, CreateVisualState(options));
			await InvokeAppearingAsync(popupView);
			await InvokeAppearingAsync(viewModel);
			this.RaisePopupStackChanged();
			return session;
		}

		private async Task PopTopAsync()
		{
			if (this.popupStack.Count == 0)
				return;
			PopupSession session = this.popupStack.Pop();
			ContentView popupView = session.View;
			object? bindingContext = session.BindingContext;
			try
			{
				await InvokeDisappearingAsync(popupView);
				await InvokeDisappearingAsync(bindingContext);
				if (bindingContext is BasePopupViewModel basePopupViewModel)
					await basePopupViewModel.NotifyPoppedAsync();
				IShellPresenter presenter = this.GetPresenter();
				PopupVisualState? nextVisualState = this.popupStack.Count > 0 ? CreateVisualState(this.popupStack.Peek().Options) : null;
				await presenter.HidePopup(popupView, session.Options.HideTransition, nextVisualState);
				await InvokeDisposeAsync(popupView);
				await InvokeDisposeAsync(bindingContext);
				if (bindingContext is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync();
				else if (bindingContext is IDisposable disposable)
					disposable.Dispose();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				session.Completion.TrySetResult(true);
				this.RaisePopupStackChanged();
			}
		}

		private Task<TResult> Enqueue<TResult>(Func<Task<TResult>> action)
		{
			TaskCompletionSource<TResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
			this.taskQueue.Enqueue(async () =>
			{
				try
				{
					TResult result = await action();
					completion.TrySetResult(result);
				}
				catch (Exception ex)
				{
					completion.TrySetException(ex);
				}
			});
			this.StartQueueProcessing();
			return completion.Task;
		}

		private Task Enqueue(Func<Task> action)
		{
			TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
			this.taskQueue.Enqueue(async () =>
			{
				try
				{
					await action();
					completion.TrySetResult(true);
				}
				catch (Exception ex)
				{
					completion.TrySetException(ex);
				}
			});
			this.StartQueueProcessing();
			return completion.Task;
		}

		private void StartQueueProcessing()
		{
			if (this.isExecutingQueue)
				return;
			this.isExecutingQueue = true;
			MainThread.BeginInvokeOnMainThread(async () => await this.ProcessQueueAsync());
		}

		private async Task ProcessQueueAsync()
		{
			try
			{
				while (this.taskQueue.TryDequeue(out Func<Task>? action))
					await action();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.isExecutingQueue = false;
			}
		}

		private IShellPresenter GetPresenter()
		{
			Page? mainPage = Application.Current?.MainPage;
			if (mainPage is null)
				throw new InvalidOperationException("No main page is available for popup presentation.");
			IShellPresenter? presenter = (mainPage as NavigationPage)?.CurrentPage as IShellPresenter ?? mainPage as IShellPresenter;
			if (presenter is null)
				throw new InvalidOperationException("CustomShell presenter not found.");
			if (!ReferenceEquals(this.attachedPresenter, presenter))
			{
				this.DetachPresenter();
				this.attachedPresenter = presenter;
				presenter.PopupBackgroundTapped += this.OnPresenterPopupBackgroundTapped;
				presenter.PopupBackRequested += this.OnPresenterPopupBackRequested;
			}
			return presenter;
		}

		private void DetachPresenter()
		{
			if (this.attachedPresenter is null)
				return;
			this.attachedPresenter.PopupBackgroundTapped -= this.OnPresenterPopupBackgroundTapped;
			this.attachedPresenter.PopupBackRequested -= this.OnPresenterPopupBackRequested;
			this.attachedPresenter = null;
		}

		private void OnPresenterPopupBackgroundTapped(object? sender, EventArgs args)
		{
			_ = this.Enqueue(async () =>
			{
				if (this.popupStack.Count == 0)
					return;
				PopupSession session = this.popupStack.Peek();
				if (!session.Options.CloseOnBackgroundTap)
					return;
				await this.PopTopAsync();
			});
		}

		private void OnPresenterPopupBackRequested(object? sender, EventArgs args)
		{
			_ = this.Enqueue(async () =>
			{
				if (this.popupStack.Count == 0)
					return;
				PopupSession session = this.popupStack.Peek();
				if (!session.Options.CloseOnBackButton)
					return;
				await this.PopTopAsync();
			});
		}

		private void RaisePopupStackChanged() => this.PopupStackChanged?.Invoke(this, EventArgs.Empty);

		private static PopupVisualState CreateVisualState(PopupOptions options)
		{
			return new PopupVisualState(
				options.OverlayOpacity,
				options.IsBlocking,
				options.CloseOnBackgroundTap,
				options.Placement,
				options.AnchorPoint,
				options.Margin,
				options.Padding);
		}

		private static void ApplyOptionsToPopup(ContentView popupView, PopupOptions options)
		{
			if (popupView is BasePopupView basePopup)
			{
				basePopup.Placement = options.Placement;
				basePopup.AnchorPoint = options.AnchorPoint;
				basePopup.PopupMargin = options.Margin;
				basePopup.PopupPadding = options.Padding;
				basePopup.CloseOnBackgroundTap = options.CloseOnBackgroundTap && !options.DisableBackgroundTap;
			}
		}

		private static async Task EnsureInitializedAsync(object? target)
		{
			if (target is null)
				return;
			if (target is ILifeCycleView lifeCycle)
			{
				System.Reflection.PropertyInfo? property = target.GetType().GetProperty("IsInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				bool isInitialized = false;
				if (property?.CanRead == true)
					isInitialized = (bool)(property.GetValue(target) ?? false);
				if (!isInitialized)
				{
					await lifeCycle.OnInitializeAsync();
					if (property?.CanWrite == true)
						property.SetValue(target, true);
				}
			}
		}

		private static async Task InvokeAppearingAsync(object? target)
		{
			if (target is ILifeCycleView lifeCycle)
				await lifeCycle.OnAppearingAsync();
		}

		private static async Task InvokeDisappearingAsync(object? target)
		{
			if (target is ILifeCycleView lifeCycle)
				await lifeCycle.OnDisappearingAsync();
		}

		private static async Task InvokeDisposeAsync(object? target)
		{
			if (target is ILifeCycleView lifeCycle)
				await lifeCycle.OnDisposeAsync();
		}

		private sealed class PopupSession
		{
			public PopupSession(ContentView view, object? bindingContext, PopupOptions options)
			{
				this.View = view;
				this.BindingContext = bindingContext;
				this.Options = options;
				this.Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			}
			public ContentView View { get; }
			public object? BindingContext { get; }
			public PopupOptions Options { get; }
			public TaskCompletionSource<bool> Completion { get; }
		}
	}
}
