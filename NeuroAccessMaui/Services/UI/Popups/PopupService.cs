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

		/// <inheritdoc/>
		public async Task PushAsync<TPopup>(PopupOptions? Options = null) where TPopup : ContentView
		{
			PopupOptions EffectiveOptions = Options ?? new PopupOptions();
			PopupSession Session = await this.Enqueue(async () =>
			{
				TPopup PopupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				return await this.PrepareAndShowPopupAsync(PopupView, PopupView.BindingContext, EffectiveOptions);
			});

			await Session.Completion.Task;
		}

		/// <inheritdoc/>
		public async Task PushAsync<TPopup, TViewModel>(PopupOptions? Options = null)
			where TPopup : ContentView
			where TViewModel : class
		{
			PopupOptions EffectiveOptions = Options ?? new PopupOptions();
			PopupSession Session = await this.Enqueue(async () =>
			{
				TPopup PopupView = ServiceRef.Provider.GetRequiredService<TPopup>();
				TViewModel ViewModel = ServiceRef.Provider.GetRequiredService<TViewModel>();
				PopupView.BindingContext = ViewModel;
				return await this.PrepareAndShowPopupAsync(PopupView, ViewModel, EffectiveOptions);
			});

			await Session.Completion.Task;
		}

		/// <inheritdoc/>
        public async Task<TReturn?> PushAsync<TPopup, TViewModel, TReturn>(PopupOptions? Options = null)
            where TPopup : ContentView
            where TViewModel : ReturningPopupViewModel<TReturn>
        {
            PopupOptions EffectiveOptions = Options ?? new PopupOptions();
            TViewModel ViewModel = ServiceRef.Provider.GetRequiredService<TViewModel>();
            Task<TReturn?> ResultTask = ViewModel.Result;
            await this.Enqueue(async () =>
            {
                TPopup PopupView = ServiceRef.Provider.GetRequiredService<TPopup>();
                PopupView.BindingContext = ViewModel;
                await this.PrepareAndShowPopupAsync(PopupView, ViewModel, EffectiveOptions);
            });
            return await ResultTask;
        }

        /// <inheritdoc/>
        public async Task<TReturn?> PushAsync<TPopup, TViewModel, TReturn>(TViewModel ViewModel, PopupOptions? Options = null)
            where TPopup : ContentView
            where TViewModel : ReturningPopupViewModel<TReturn>
        {
            if (ViewModel is null)
                throw new ArgumentNullException(nameof(ViewModel));

            PopupOptions EffectiveOptions = Options ?? new PopupOptions();
            Task<TReturn?> ResultTask = ViewModel.Result;
            await this.Enqueue(async () =>
            {
                TPopup PopupView = ServiceRef.Provider.GetRequiredService<TPopup>();
                PopupView.BindingContext = ViewModel;
                await this.PrepareAndShowPopupAsync(PopupView, ViewModel, EffectiveOptions);
            });
            return await ResultTask;
        }

		/// <inheritdoc/>
		public async Task PushAsync(ContentView Popup, PopupOptions? Options = null)
		{
			if (Popup is null)
				throw new ArgumentNullException(nameof(Popup));

			PopupOptions EffectiveOptions = Options ?? new PopupOptions();
			PopupSession Session = await this.Enqueue(async () => await this.PrepareAndShowPopupAsync(Popup, Popup.BindingContext, EffectiveOptions));

			await Session.Completion.Task;
		}

		/// <inheritdoc/>
		public Task PopAsync()
		{
			return this.Enqueue(this.PopTopAsync);
		}

		private async Task<PopupSession> PrepareAndShowPopupAsync(ContentView PopupView, object? ViewModel, PopupOptions Options)
		{
			await EnsureInitializedAsync(PopupView);
			await EnsureInitializedAsync(ViewModel);

			PopupSession Session = new PopupSession(PopupView, ViewModel, Options);
			this.popupStack.Push(Session);

			IShellPresenter Presenter = this.GetPresenter();
			await Presenter.ShowPopup(PopupView, Options.ShowTransition, Options.OverlayOpacity);

			await InvokeAppearingAsync(PopupView);
			await InvokeAppearingAsync(ViewModel);

			this.RaisePopupStackChanged();
			return Session;
		}

		private async Task PopTopAsync()
		{
			if (this.popupStack.Count == 0)
				return;

			PopupSession Session = this.popupStack.Pop();
			ContentView PopupView = Session.View;
			object? BindingContext = Session.BindingContext;

			try
			{
				await InvokeDisappearingAsync(PopupView);
				await InvokeDisappearingAsync(BindingContext);

            if (BindingContext is BasePopupViewModel BasePopupViewModel)
                await BasePopupViewModel.NotifyPoppedAsync();

				IShellPresenter Presenter = this.GetPresenter();
				await Presenter.HideTopPopup(Session.Options.HideTransition);

				await InvokeDisposeAsync(PopupView);
				await InvokeDisposeAsync(BindingContext);

				if (BindingContext is IAsyncDisposable AsyncDisposable)
					await AsyncDisposable.DisposeAsync();
				else if (BindingContext is IDisposable Disposable)
					Disposable.Dispose();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				Session.Completion.TrySetResult(true);
				this.RaisePopupStackChanged();
			}
		}

		private Task<TResult> Enqueue<TResult>(Func<Task<TResult>> Action)
		{
			TaskCompletionSource<TResult> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

			this.taskQueue.Enqueue(async () =>
			{
				try
				{
					TResult Result = await Action();
					Completion.TrySetResult(Result);
				}
				catch (Exception Ex)
				{
					Completion.TrySetException(Ex);
				}
			});

			this.StartQueueProcessing();
			return Completion.Task;
		}

		private Task Enqueue(Func<Task> Action)
		{
			TaskCompletionSource<bool> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

			this.taskQueue.Enqueue(async () =>
			{
				try
				{
					await Action();
					Completion.TrySetResult(true);
				}
				catch (Exception Ex)
				{
					Completion.TrySetException(Ex);
				}
			});

			this.StartQueueProcessing();
			return Completion.Task;
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
				while (this.taskQueue.TryDequeue(out Func<Task>? Action))
					await Action();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.isExecutingQueue = false;
			}
		}

		private IShellPresenter GetPresenter()
		{
			Page? MainPage = Application.Current?.MainPage;
			if (MainPage is null)
				throw new InvalidOperationException("No main page is available for popup presentation.");

			IShellPresenter? Presenter = (MainPage as NavigationPage)?.CurrentPage as IShellPresenter ?? MainPage as IShellPresenter;
			if (Presenter is null)
				throw new InvalidOperationException("CustomShell presenter not found.");

			if (!ReferenceEquals(this.attachedPresenter, Presenter))
			{
				this.DetachPresenter();
				this.attachedPresenter = Presenter;
				Presenter.PopupBackgroundTapped += this.OnPresenterPopupBackgroundTapped;
				Presenter.PopupBackRequested += this.OnPresenterPopupBackRequested;
			}

			return Presenter;
		}

		private void DetachPresenter()
		{
			if (this.attachedPresenter is null)
				return;

			this.attachedPresenter.PopupBackgroundTapped -= this.OnPresenterPopupBackgroundTapped;
			this.attachedPresenter.PopupBackRequested -= this.OnPresenterPopupBackRequested;
			this.attachedPresenter = null;
		}

		private void OnPresenterPopupBackgroundTapped(object? Sender, EventArgs Args)
		{
			_ = this.Enqueue(async () =>
			{
				if (this.popupStack.Count == 0)
					return;

				PopupSession Session = this.popupStack.Peek();
				if (!Session.Options.CloseOnBackgroundTap)
					return;

				await this.PopTopAsync();
			});
		}

		private void OnPresenterPopupBackRequested(object? Sender, EventArgs Args)
		{
			_ = this.Enqueue(async () =>
			{
				if (this.popupStack.Count == 0)
					return;

				PopupSession Session = this.popupStack.Peek();
				if (!Session.Options.CloseOnBackButton)
					return;

				await this.PopTopAsync();
			});
		}

		private void RaisePopupStackChanged()
		{
			PopupStackChanged?.Invoke(this, EventArgs.Empty);
		}

		private static async Task EnsureInitializedAsync(object? Target)
		{
			if (Target is null)
				return;

			if (Target is ILifeCycleView LifeCycle)
			{
				Type TargetType = Target.GetType();
				System.Reflection.PropertyInfo? Property = TargetType.GetProperty("IsInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				bool IsInitialized = false;
				if (Property?.CanRead == true)
					IsInitialized = (bool)(Property.GetValue(Target) ?? false);

				if (!IsInitialized)
				{
					await LifeCycle.OnInitializeAsync();
					if (Property?.CanWrite == true)
						Property.SetValue(Target, true);
				}
			}
		}

		private static async Task InvokeAppearingAsync(object? Target)
		{
			if (Target is ILifeCycleView LifeCycle)
				await LifeCycle.OnAppearingAsync();
		}

		private static async Task InvokeDisappearingAsync(object? Target)
		{
			if (Target is ILifeCycleView LifeCycle)
				await LifeCycle.OnDisappearingAsync();
		}

		private static async Task InvokeDisposeAsync(object? Target)
		{
			if (Target is ILifeCycleView LifeCycle)
				await LifeCycle.OnDisposeAsync();
		}

		private sealed class PopupSession
		{
			public PopupSession(ContentView View, object? BindingContext, PopupOptions Options)
			{
				this.View = View;
				this.BindingContext = BindingContext;
				this.Options = Options;
				this.Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			public ContentView View { get; }

			public object? BindingContext { get; }

			public PopupOptions Options { get; }

			public TaskCompletionSource<bool> Completion { get; }
		}
	}
}
