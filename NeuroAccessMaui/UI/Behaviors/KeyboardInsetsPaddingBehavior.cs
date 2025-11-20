using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using ControlsVisualElement = Microsoft.Maui.Controls.VisualElement;

namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// Applies the current keyboard inset to the padding of the associated view.
	/// </summary>
	public class KeyboardInsetsPaddingBehavior : Behavior<ControlsVisualElement>
	{
		private Thickness originalPadding;
		private IKeyboardInsetsService? keyboardInsetsService;
		private ControlsVisualElement? associatedView;

		/// <summary>
		/// Identifies the <see cref="Animate"/> bindable property.
		/// </summary>
		public static readonly BindableProperty AnimateProperty =
			BindableProperty.Create(nameof(Animate), typeof(bool), typeof(KeyboardInsetsPaddingBehavior), true);

		/// <summary>
		/// Identifies the <see cref="AdditionalBottomPadding"/> bindable property.
		/// </summary>
		public static readonly BindableProperty AdditionalBottomPaddingProperty =
			BindableProperty.Create(nameof(AdditionalBottomPadding), typeof(double), typeof(KeyboardInsetsPaddingBehavior), 0d);

		/// <summary>
		/// Gets or sets a value indicating whether padding changes should be animated.
		/// </summary>
		public bool Animate
		{
			get => (bool)this.GetValue(AnimateProperty);
			set => this.SetValue(AnimateProperty, value);
		}

		/// <summary>
		/// Gets or sets an additional bottom padding offset that is always added to the keyboard height.
		/// </summary>
		public double AdditionalBottomPadding
		{
			get => (double)this.GetValue(AdditionalBottomPaddingProperty);
			set => this.SetValue(AdditionalBottomPaddingProperty, value);
		}

		/// <inheritdoc/>
		protected override void OnAttachedTo(ControlsVisualElement bindable)
		{
			base.OnAttachedTo(bindable);
			this.associatedView = bindable;
			this.originalPadding = GetPadding(bindable);
			this.keyboardInsetsService = ServiceRef.KeyboardInsetsService;
			this.keyboardInsetsService.KeyboardInsetChanged += this.OnKeyboardInsetChanged;
			this.ApplyPadding(bindable, this.keyboardInsetsService.KeyboardHeight, false);
		}

		/// <inheritdoc/>
		protected override void OnDetachingFrom(ControlsVisualElement bindable)
		{
			base.OnDetachingFrom(bindable);
			if (this.keyboardInsetsService is not null)
				this.keyboardInsetsService.KeyboardInsetChanged -= this.OnKeyboardInsetChanged;
			SetPadding(bindable, this.originalPadding);
			this.keyboardInsetsService = null;
			this.associatedView = null;
		}

		private void OnKeyboardInsetChanged(object? sender, KeyboardInsetChangedEventArgs e)
		{
			if (this.associatedView is not ControlsVisualElement view)
				return;

			this.ApplyPadding(view, e.KeyboardHeight, this.Animate);
		}

		private void ApplyPadding(ControlsVisualElement view, double keyboardHeight, bool animate)
		{
			double totalBottom = this.originalPadding.Bottom + keyboardHeight + this.AdditionalBottomPadding;
			Thickness targetPadding = new Thickness(
				this.originalPadding.Left,
				this.originalPadding.Top,
				this.originalPadding.Right,
				totalBottom);

			if (!animate)
			{
				SetPadding(view, targetPadding);
				return;
			}

			Thickness currentPadding = GetPadding(view);
			if (this.AreThicknessClose(currentPadding, targetPadding))
			{
				SetPadding(view, targetPadding);
				return;
			}

			const uint duration = 180;
			Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(view);
			view.Animate(
				"KeyboardInsetsPadding",
				progress => SetPadding(view, this.InterpolateThickness(currentPadding, targetPadding, progress)),
				rate: 16,
				length: duration,
				easing: Easing.CubicInOut,
				finished: (v, cancelled) => SetPadding(view, targetPadding));
		}

		private static void SetPadding(ControlsVisualElement element, Thickness padding)
		{
			switch (element)
			{
				case Layout layout:
					layout.Padding = padding;
					break;
				case ContentView contentView:
					contentView.Padding = padding;
					break;
				case TemplatedView templatedView:
					templatedView.Padding = padding;
					break;
				case Border border:
					border.Padding = padding;
					break;
			}
		}

		private static Thickness GetPadding(ControlsVisualElement element)
		{
			return element switch
			{
				Layout layout => layout.Padding,
				ContentView contentView => contentView.Padding,
				TemplatedView templatedView => templatedView.Padding,
				Border border => border.Padding,
				_ => new Thickness(0)
			};
		}

		private bool AreThicknessClose(Thickness a, Thickness b)
		{
			return Math.Abs(a.Left - b.Left) < 0.5 &&
				   Math.Abs(a.Top - b.Top) < 0.5 &&
				   Math.Abs(a.Right - b.Right) < 0.5 &&
				   Math.Abs(a.Bottom - b.Bottom) < 0.5;
		}

		private Thickness InterpolateThickness(Thickness from, Thickness to, double progress)
		{
			return new Thickness(
				this.InterpolateDouble(from.Left, to.Left, progress),
				this.InterpolateDouble(from.Top, to.Top, progress),
				this.InterpolateDouble(from.Right, to.Right, progress),
				this.InterpolateDouble(from.Bottom, to.Bottom, progress));
		}

		private double InterpolateDouble(double from, double to, double progress)
		{
			return from + ((to - from) * progress);
		}
	}
}
