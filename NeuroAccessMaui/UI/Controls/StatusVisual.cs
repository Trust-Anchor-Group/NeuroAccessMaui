using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Shows an animated, theme-aware status badge: a draft pencil, a pending hourglass, a success check mark,
	/// or an attention mark.
	/// </summary>
	/// <remarks>
	/// Changing <see cref="Kind"/> or loading the view plays a short entrance. <see cref="Celebrate"/> adds a ripple and
	/// particle burst for moments worth marking, such as sending an application. The pending scene loops only while the
	/// view is loaded and <see cref="IsAnimationActive"/> is set. With reduced motion the settled frame is drawn and
	/// nothing animates. Drawing is delegated to <see cref="StatusVisualPainter"/>.
	/// </remarks>
	public class StatusVisual : GraphicsView, IDrawable
	{
		private const string entranceAnimationName = "StatusVisualEntrance";
		private const string loopAnimationName = "StatusVisualLoop";
		private const string celebrationAnimationName = "StatusVisualCelebration";
		private const uint frameRate = 16;
		private const uint entranceLength = 560;
		private const uint loopLength = 2800;
		private const uint celebrationLength = 900;

		private static readonly Color fallbackAccent = Color.FromArgb("#085E50");
		private static readonly Color fallbackOnAccent = Color.FromArgb("#F5F6F7");
		private static readonly Color fallbackWarning = Color.FromArgb("#A84700");

		private double entranceProgress = 1;
		private double loopPhase;
		private double celebrationProgress = -1;
		private bool isLoaded;
		private bool isLoopRunning;

		/// <summary>
		/// Identifies the <see cref="Kind"/> bindable property.
		/// </summary>
		public static readonly BindableProperty KindProperty = BindableProperty.Create(
			nameof(Kind),
			typeof(StatusVisualKind),
			typeof(StatusVisual),
			StatusVisualKind.Pending,
			propertyChanged: (Bindable, OldValue, NewValue) => ((StatusVisual)Bindable).OnKindChanged());

		/// <summary>
		/// Identifies the <see cref="IsAnimationActive"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IsAnimationActiveProperty = BindableProperty.Create(
			nameof(IsAnimationActive),
			typeof(bool),
			typeof(StatusVisual),
			true,
			propertyChanged: (Bindable, OldValue, NewValue) => ((StatusVisual)Bindable).UpdateLoop());

		/// <summary>
		/// Identifies the <see cref="AccentColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty AccentColorProperty = CreateColorProperty(nameof(AccentColor), fallbackAccent);

		/// <summary>
		/// Identifies the <see cref="OnAccentColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty OnAccentColorProperty = CreateColorProperty(nameof(OnAccentColor), fallbackOnAccent);

		/// <summary>
		/// Identifies the <see cref="WarningColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty WarningColorProperty = CreateColorProperty(nameof(WarningColor), fallbackWarning);

		/// <summary>
		/// Initializes a new instance of the <see cref="StatusVisual"/> class.
		/// </summary>
		public StatusVisual()
		{
			this.Drawable = this;
			this.InputTransparent = true;
			AutomationProperties.SetIsInAccessibleTree(this, false);
			this.Loaded += this.OnLoaded;
			this.Unloaded += this.OnUnloaded;
		}

		/// <summary>
		/// Gets or sets the scene to draw.
		/// </summary>
		public StatusVisualKind Kind
		{
			get => (StatusVisualKind)this.GetValue(KindProperty);
			set => this.SetValue(KindProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether looping animation may run. Pages clear it while hidden.
		/// </summary>
		public bool IsAnimationActive
		{
			get => (bool)this.GetValue(IsAnimationActiveProperty);
			set => this.SetValue(IsAnimationActiveProperty, value);
		}

		/// <summary>
		/// Gets or sets the color for the disk, glyphs, and celebration.
		/// </summary>
		public Color AccentColor
		{
			get => (Color)this.GetValue(AccentColorProperty);
			set => this.SetValue(AccentColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the color of the check mark drawn on the filled success disk.
		/// </summary>
		public Color OnAccentColor
		{
			get => (Color)this.GetValue(OnAccentColorProperty);
			set => this.SetValue(OnAccentColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the color used by the attention scene.
		/// </summary>
		public Color WarningColor
		{
			get => (Color)this.GetValue(WarningColorProperty);
			set => this.SetValue(WarningColorProperty, value);
		}

		private static bool IsReducedMotion
		{
			get
			{
				try
				{
					return ServiceRef.Provider?.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		private bool CanAnimate => this.isLoaded && this.Handler is not null && !IsReducedMotion;

		private bool ShouldLoop => this.Kind == StatusVisualKind.Pending && this.IsAnimationActive && this.CanAnimate;

		/// <summary>
		/// Replays the entrance and adds a ripple and particle burst. Does nothing visible beyond a redraw with reduced motion.
		/// </summary>
		public void Celebrate()
		{
			if (!this.CanAnimate)
			{
				this.Invalidate();
				return;
			}

			this.PlayEntrance();
			this.AbortAnimation(celebrationAnimationName);
			this.celebrationProgress = 0;
			this.RunAnimation(
				celebrationAnimationName,
				Value => this.celebrationProgress = Value,
				celebrationLength,
				() => this.celebrationProgress = -1);
		}

		/// <inheritdoc/>
		public void Draw(ICanvas Canvas, RectF DirtyRect)
		{
			StatusVisualFrame Frame = new StatusVisualFrame(
				this.Kind,
				this.entranceProgress,
				this.loopPhase,
				this.celebrationProgress,
				this.AccentColor ?? fallbackAccent,
				this.OnAccentColor ?? fallbackOnAccent,
				this.WarningColor ?? fallbackWarning);

			StatusVisualPainter.Draw(Canvas, DirtyRect, Frame);
		}

		private static BindableProperty CreateColorProperty(string Name, Color Fallback)
		{
			return BindableProperty.Create(
				Name,
				typeof(Color),
				typeof(StatusVisual),
				Fallback,
				propertyChanged: (Bindable, OldValue, NewValue) => ((StatusVisual)Bindable).Invalidate());
		}

		private void OnKindChanged()
		{
			this.PlayEntrance();
			this.UpdateLoop();
		}

		private void OnLoaded(object? Sender, EventArgs E)
		{
			this.isLoaded = true;
			this.PlayEntrance();
			this.UpdateLoop();
		}

		private void OnUnloaded(object? Sender, EventArgs E)
		{
			this.isLoaded = false;
			this.AbortAnimation(entranceAnimationName);
			this.AbortAnimation(celebrationAnimationName);
			this.AbortAnimation(loopAnimationName);
			this.isLoopRunning = false;
			this.entranceProgress = 1;
			this.celebrationProgress = -1;
			this.loopPhase = 0;
		}

		private void PlayEntrance()
		{
			this.AbortAnimation(entranceAnimationName);

			if (!this.CanAnimate)
			{
				this.entranceProgress = 1;
				this.Invalidate();
				return;
			}

			this.entranceProgress = 0;
			this.RunAnimation(entranceAnimationName, Value => this.entranceProgress = Value, entranceLength, () => this.entranceProgress = 1);
		}

		private void UpdateLoop()
		{
			bool Run = this.ShouldLoop;
			if (Run == this.isLoopRunning)
				return;

			if (!Run)
			{
				this.AbortAnimation(loopAnimationName);
				this.isLoopRunning = false;
				this.loopPhase = 0;
				this.Invalidate();
				return;
			}

			try
			{
				this.isLoopRunning = true;
				this.Animate(
					loopAnimationName,
					Value =>
					{
						this.loopPhase = Value;
						this.Invalidate();
					},
					0,
					1,
					frameRate,
					loopLength,
					Easing.Linear,
					repeat: () =>
					{
						bool Continue = this.ShouldLoop;
						if (!Continue)
							this.isLoopRunning = false;
						return Continue;
					});
			}
			catch (Exception)
			{
				this.isLoopRunning = false;
				this.loopPhase = 0;
				this.Invalidate();
			}
		}

		private void RunAnimation(string Name, Action<double> Update, uint Length, Action Completed)
		{
			try
			{
				this.Animate(
					Name,
					Value =>
					{
						Update(Value);
						this.Invalidate();
					},
					0,
					1,
					frameRate,
					Length,
					Easing.Linear,
					(Value, Cancelled) =>
					{
						Completed();
						this.Invalidate();
					});
			}
			catch (Exception)
			{
				Completed();
				this.Invalidate();
			}
		}
	}
}
