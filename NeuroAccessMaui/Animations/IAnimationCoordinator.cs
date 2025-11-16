using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Coordinates animation execution across the application.
	/// </summary>
	public interface IAnimationCoordinator
	{
		/// <summary>
		/// Event raised when an animation lifecycle change occurs.
		/// </summary>
		event EventHandler<AnimationEvent>? AnimationEvent;

		/// <summary>
		/// Plays a view animation associated with the specified key.
		/// </summary>
		/// <param name="Key">Animation identifier.</param>
		/// <param name="Target">Target view.</param>
		/// <param name="Options">Optional execution options.</param>
		/// <param name="ContextOptions">Optional context overrides.</param>
		/// <param name="Token">Cancellation token.</param>
		/// <returns>Task representing the execution.</returns>
		Task PlayAsync(AnimationKey Key, VisualElement Target, AnimationOptions? Options = null, AnimationContextOptions? ContextOptions = null, CancellationToken Token = default);

		/// <summary>
		/// Plays a transition animation associated with the specified key.
		/// </summary>
		/// <param name="Key">Animation identifier.</param>
		/// <param name="Entering">Entering view.</param>
		/// <param name="Exiting">Exiting view.</param>
		/// <param name="Options">Optional execution options.</param>
		/// <param name="ContextOptions">Optional context overrides.</param>
		/// <param name="Token">Cancellation token.</param>
		/// <returns>Task representing the execution.</returns>
		Task PlayTransitionAsync(AnimationKey Key, VisualElement? Entering, VisualElement? Exiting, AnimationOptions? Options = null, AnimationContextOptions? ContextOptions = null, CancellationToken Token = default);
	}

	/// <summary>
	/// Default implementation of <see cref="IAnimationCoordinator"/>.
	/// </summary>
	public sealed class AnimationCoordinator : IAnimationCoordinator
	{
		private readonly IAnimationRegistry registry;
		private readonly IAnimationContextProvider contextProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationCoordinator"/> class.
		/// </summary>
		/// <param name="Registry">Animation registry.</param>
		/// <param name="ContextProvider">Context provider.</param>
		public AnimationCoordinator(IAnimationRegistry Registry, IAnimationContextProvider ContextProvider)
		{
			this.registry = Registry ?? throw new ArgumentNullException(nameof(Registry));
			this.contextProvider = ContextProvider ?? throw new ArgumentNullException(nameof(ContextProvider));
		}

		/// <inheritdoc/>
		public event EventHandler<AnimationEvent>? AnimationEvent;

		/// <inheritdoc/>
		public async Task PlayAsync(AnimationKey Key, VisualElement Target, AnimationOptions? Options = null, AnimationContextOptions? ContextOptions = null, CancellationToken Token = default)
		{
			ArgumentNullException.ThrowIfNull(Target);
			IAnimationContext Context = this.contextProvider.CreateContext(ContextOptions);

			if (!this.registry.TryCreateViewAnimation(Key, Context, out IViewAnimation Animation))
			{
				this.RaiseEvent(new AnimationEvent(Key, AnimationLifecycleStage.Skipped, TimeSpan.Zero, false, null));
				Options?.Completed?.Invoke();
				return;
			}

			Func<CancellationToken, Task> Execute = CancellationToken => EnsureMainThreadAsync(() => Animation.RunAsync(Target, Context, Options, CancellationToken));

			await this.ExecuteAsync(Key, Target, Options, Execute, Token);
		}

		/// <inheritdoc/>
		public async Task PlayTransitionAsync(AnimationKey Key, VisualElement? Entering, VisualElement? Exiting, AnimationOptions? Options = null, AnimationContextOptions? ContextOptions = null, CancellationToken Token = default)
		{
			if (Entering is null && Exiting is null)
				throw new ArgumentException("At least one visual element must be provided.", nameof(Entering));

			IAnimationContext Context = this.contextProvider.CreateContext(ContextOptions);

			if (!this.registry.TryCreateTransition(Key, Context, out ITransitionAnimation Animation))
			{
				this.RaiseEvent(new AnimationEvent(Key, AnimationLifecycleStage.Skipped, TimeSpan.Zero, false, null));
				Options?.Completed?.Invoke();
				return;
			}

			VisualElement Anchor = Entering ?? Exiting!;
			Func<CancellationToken, Task> Execute = CancellationToken => EnsureMainThreadAsync(() => Animation.RunAsync(Entering, Exiting, Context, Options, CancellationToken));

			await this.ExecuteAsync(Key, Anchor, Options, Execute, Token);
		}

		private async Task ExecuteAsync(AnimationKey Key, VisualElement Anchor, AnimationOptions? Options, Func<CancellationToken, Task> Execute, CancellationToken Token)
		{
			Stopwatch Stopwatch = Stopwatch.StartNew();
			this.RaiseEvent(new AnimationEvent(Key, AnimationLifecycleStage.Started, TimeSpan.Zero, false, null));
			try
			{
				await AnimationRunner.RunAsync(Anchor, Execute, Token);
				Stopwatch.Stop();
				this.RaiseEvent(new AnimationEvent(Key, AnimationLifecycleStage.Completed, Stopwatch.Elapsed, false, null));
				Options?.Completed?.Invoke();
			}
			catch (OperationCanceledException)
			{
				Stopwatch.Stop();
				this.RaiseEvent(new AnimationEvent(Key, AnimationLifecycleStage.Cancelled, Stopwatch.Elapsed, true, null));
				throw;
			}
			catch (Exception Ex)
			{
				Stopwatch.Stop();
				this.RaiseEvent(new AnimationEvent(Key, AnimationLifecycleStage.Failed, Stopwatch.Elapsed, false, Ex));
				throw;
			}
		}

		private static Task EnsureMainThreadAsync(Func<Task> Action)
		{
			if (MainThread.IsMainThread)
				return Action();

			return MainThread.InvokeOnMainThreadAsync(Action);
		}

		private void RaiseEvent(AnimationEvent Event)
		{
			this.AnimationEvent?.Invoke(this, Event);
		}
	}
}
