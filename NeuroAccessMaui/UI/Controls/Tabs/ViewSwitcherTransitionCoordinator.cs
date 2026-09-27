using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Animations;

namespace NeuroAccessMaui.UI.Controls
{
	internal partial class ViewSwitcherTransitionCoordinator : IDisposable
	{
		private readonly Grid presenter;
		private readonly IAnimationCoordinator? animationCoordinator;
		private readonly SemaphoreSlim transitionLock = new SemaphoreSlim(1, 1);
		private CancellationTokenSource? transitionSource;
		private volatile bool disposed;

		public ViewSwitcherTransitionCoordinator(Grid presenter, IAnimationCoordinator? animationCoordinator)
		{
			this.presenter = presenter;
			this.animationCoordinator = animationCoordinator;
			this.Transition = new CrossFadeViewTransition();
			this.Animate = true;
			this.Duration = 250;
			this.Easing = Easing.Linear;
		}

		public IViewTransition Transition { get; set; }

		public bool Animate { get; set; }

		public uint Duration { get; set; }

		public Easing? Easing { get; set; }

		public View? CurrentView { get; private set; }

		/// <summary>
		/// Presents the next view, cancelling any superseded transition.
		/// </summary>
		/// <param name="owner">The view switcher requesting the transition.</param>
		/// <param name="nextView">The view to present.</param>
		/// <param name="isInitial">Whether this is the initial presentation.</param>
		/// <param name="cancellationToken">Token used to cancel the transition.</param>
		/// <returns>A task representing the transition and its cleanup.</returns>
		public async Task SwitchAsync(ViewSwitcher owner, View? nextView, bool isInitial, CancellationToken cancellationToken)
		{
			if (this.disposed)
				throw new ObjectDisposedException(nameof(ViewSwitcherTransitionCoordinator));

			using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			CancellationToken TransitionToken = linkedSource.Token;
			CancellationTokenSource? previous = Interlocked.Exchange(ref this.transitionSource, linkedSource);
			if (previous is not null)
			{
				try
				{
					previous.Cancel();
				}
				catch (ObjectDisposedException)
				{
				}
			}

			bool LockAcquired = false;
			try
			{
				if (this.disposed)
					linkedSource.Cancel();

				await this.transitionLock.WaitAsync(TransitionToken).ConfigureAwait(false);
				LockAcquired = true;
				TransitionToken.ThrowIfCancellationRequested();
				View? currentView = this.CurrentView;
				if (ReferenceEquals(currentView, nextView))
					return;

				await owner.Dispatcher.DispatchAsync(async () =>
				{
					TransitionToken.ThrowIfCancellationRequested();
					if (nextView is not null)
					{
						this.DetachFromParent(nextView);
						if (!this.presenter.Children.Contains(nextView))
						{
							this.presenter.Children.Add(nextView);
						}
					}

					ViewSwitcherTransitionRequest request = new ViewSwitcherTransitionRequest(
						owner,
						currentView,
						nextView,
						isInitial,
						this.Animate,
						this.Duration,
						this.Easing);

					bool transitionCompleted = false;
					try
					{
						if (!this.Animate)
						{
							transitionCompleted = true;
						}
						else if (this.ShouldUseCoordinator())
						{
							AnimationOptions? Options = null;
							if (this.Duration > 0)
							{
								Options = new AnimationOptions
								{
									DurationOverride = TimeSpan.FromMilliseconds(this.Duration)
								};
							}
							AnimationContextOptions ContextOptions = this.CreateContextOptions();
							await this.animationCoordinator!.PlayTransitionAsync(AnimationKeys.ViewSwitcher.CrossFade, nextView as VisualElement, currentView as VisualElement, Options, ContextOptions, TransitionToken);
							transitionCompleted = true;
						}
						else
						{
							await this.Transition.RunAsync(request, TransitionToken);
							transitionCompleted = true;
						}
					}
					catch (OperationCanceledException)
					{
						this.CancelAnimations(currentView, nextView);
						throw;
					}
					finally
					{
						if (transitionCompleted)
						{
							if (currentView is not null && !ReferenceEquals(currentView, nextView))
							{
								this.RemoveFromPresenter(currentView);
							}
							this.CurrentView = nextView;
						}
						else
						{
							if (nextView is not null && !ReferenceEquals(currentView, nextView))
							{
								this.RemoveFromPresenter(nextView);
							}
						}
					}
				});
			}
			finally
			{
				if (LockAcquired)
					this.transitionLock.Release();

				Interlocked.CompareExchange(ref this.transitionSource, null, linkedSource);
			}
		}

		private void DetachFromParent(View view)
		{
			Element? parent = view.Parent;
			if (parent is null)
				return;

			try
			{
				if (parent is Layout layout)
				{
					layout.Children.Remove(view);
				}
				else if (parent is ContentView contentView)
				{
					if (ReferenceEquals(contentView.Content, view))
					{
						contentView.Content = null;
					}
				}
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				// WinUI can throw when gesture handlers are still tearing down. Retry by disconnecting the handler first.
				view.Handler?.DisconnectHandler();
				if (parent is Layout layout)
				{
					if (layout.Children.Contains(view))
					{
						layout.Children.Remove(view);
					}
				}
				else if (parent is ContentView contentView)
				{
					if (ReferenceEquals(contentView.Content, view))
					{
						contentView.Content = null;
					}
				}
			}

			view.Handler?.DisconnectHandler();
		}

		private void CancelAnimations(View? oldView, View? newView)
		{
			if (oldView is not null)
				Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(oldView);

			if (newView is not null)
				Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(newView);
		}

		private bool ShouldUseCoordinator()
		{
			if (this.animationCoordinator is null)
				return false;

			return this.Transition is CrossFadeViewTransition;
		}

		private AnimationContextOptions CreateContextOptions()
		{
			double? ViewportWidth = this.presenter.Width > 0 ? this.presenter.Width : null;
			double? ViewportHeight = this.presenter.Height > 0 ? this.presenter.Height : null;
			return new AnimationContextOptions
			{
				ViewportWidth = ViewportWidth,
				ViewportHeight = ViewportHeight
			};
		}

		private void RemoveFromPresenter(View view)
		{
			if (view is null)
				return;

			bool removed = this.TryRemove(view);
			if (!removed)
			{
				view.Handler?.DisconnectHandler();
				this.TryRemove(view);
			}
		}

		private bool TryRemove(View view)
		{
			try
			{
				if (this.presenter.Children.Contains(view))
				{
					this.presenter.Children.Remove(view);
				}
				return true;
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				return false;
			}
		}

		/// <summary>
		/// Cancels the active transition and prevents further transitions.
		/// </summary>
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;
				CancellationTokenSource? source = Interlocked.Exchange(ref this.transitionSource, null);
				if (source is not null)
				{
					try
					{
						source.Cancel();
					}
					catch (ObjectDisposedException)
					{
					}
				}

				// Each transition disposes its own source after unwinding. Pending transitions
				// still need the semaphore; its managed resources can be garbage collected.
			}
		}
	}
}
