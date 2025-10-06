using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Test;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI.Toasts
{
	/// <inheritdoc/>
	[Singleton]
	public class ToastService : IToastService
	{
		private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
		private bool isExecutingQueue;
		private View? activeToast;
		private ToastOptions? activeOptions;
		private CancellationTokenSource? autoHideTokenSource;

		/// <inheritdoc/>
		public event EventHandler? ToastChanged;

		/// <inheritdoc/>
		public bool HasActiveToast => this.activeToast is not null;

		/// <inheritdoc/>
		public Task ShowAsync<TToast>(ToastOptions? Options = null) where TToast : View
		{
			TToast Toast = ServiceRef.Provider.GetRequiredService<TToast>();
			return this.ShowAsync(Toast, Options);
		}

		/// <inheritdoc/>
		public Task ShowAsync(View Toast, ToastOptions? Options = null)
		{
			if (Toast is null)
				throw new ArgumentNullException(nameof(Toast));

			ToastOptions EffectiveOptions = Options ?? new ToastOptions();
			return this.Enqueue(async () => await this.ShowInternalAsync(Toast, EffectiveOptions));
		}

		/// <inheritdoc/>
		public Task HideAsync()
		{
			return this.Enqueue(async () => await this.HideInternalAsync(null, false));
		}

		private async Task ShowInternalAsync(View Toast, ToastOptions Options)
		{
			await this.HideInternalAsync(null, true);

			this.autoHideTokenSource?.Cancel();
			this.autoHideTokenSource = null;

			this.activeToast = Toast;
			this.activeOptions = Options;

			IShellPresenter Presenter = this.GetPresenter();
			await Presenter.ShowToast(Toast, Options.ShowTransition, Options.Placement);
			this.RaiseToastChanged();

			if (Options.AutoDismiss)
				this.ScheduleAutoHide(Options);
		}

		private async Task HideInternalAsync(ToastOptions? Options, bool IsReplacing)
		{
			if (this.activeToast is null)
				return;

			ToastOptions EffectiveOptions = Options ?? this.activeOptions ?? new ToastOptions();
			this.autoHideTokenSource?.Cancel();
			this.autoHideTokenSource = null;

			try
			{
				IShellPresenter Presenter = this.GetPresenter();
				await Presenter.HideToast(EffectiveOptions.HideTransition);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			this.activeToast = null;
			this.activeOptions = null;

			if (!IsReplacing)
				this.RaiseToastChanged();
		}

		private void ScheduleAutoHide(ToastOptions Options)
		{
			CancellationTokenSource TokenSource = new();
			this.autoHideTokenSource = TokenSource;

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(Options.Duration, TokenSource.Token);
					if (!TokenSource.Token.IsCancellationRequested)
						await this.Enqueue(async () => await this.HideInternalAsync(Options, false));
				}
				catch (TaskCanceledException)
				{
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
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
				throw new InvalidOperationException("No main page is available for toast presentation.");

			IShellPresenter? Presenter = (MainPage as NavigationPage)?.CurrentPage as IShellPresenter ?? MainPage as IShellPresenter;
			if (Presenter is null)
				throw new InvalidOperationException("CustomShell presenter not found.");

			return Presenter;
		}

		private void RaiseToastChanged()
		{
			ToastChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
