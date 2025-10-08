using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	internal partial class ViewSwitcherTransitionCoordinator : IDisposable
	{
		private readonly Grid presenter;
		private readonly SemaphoreSlim transitionLock = new SemaphoreSlim(1, 1);
		private CancellationTokenSource? transitionSource;
		private bool disposed;

		public ViewSwitcherTransitionCoordinator(Grid presenter)
		{
			this.presenter = presenter;
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

		public async Task SwitchAsync(ViewSwitcher owner, View? nextView, bool isInitial, CancellationToken cancellationToken)
		{
			if (this.disposed)
				throw new ObjectDisposedException(nameof(ViewSwitcherTransitionCoordinator));

			CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
				finally
				{
					previous.Dispose();
				}
			}

			await this.transitionLock.WaitAsync(linkedSource.Token).ConfigureAwait(false);
			try
			{
				View? currentView = this.CurrentView;
				if (ReferenceEquals(currentView, nextView))
					return;

				await owner.Dispatcher.DispatchAsync(async () =>
				{
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
						await this.Transition.RunAsync(request, linkedSource.Token).ConfigureAwait(false);
						transitionCompleted = true;
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
								this.presenter.Children.Remove(currentView);
							}
							this.CurrentView = nextView;
						}
						else
						{
							if (nextView is not null && !ReferenceEquals(currentView, nextView))
							{
								this.presenter.Children.Remove(nextView);
							}
						}
					}
				}).ConfigureAwait(false);
			}
			finally
			{
				this.transitionLock.Release();

				if (Interlocked.CompareExchange(ref this.transitionSource, null, linkedSource) == linkedSource)
				{
					linkedSource.Dispose();
				}
				else
				{
					linkedSource.Dispose();
				}
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

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.transitionLock.Dispose();
				CancellationTokenSource? source = this.transitionSource;
				if (source is not null)
				{
					source.Cancel();
					source.Dispose();
				}
				this.disposed = true;
			}
		}
	}
}
