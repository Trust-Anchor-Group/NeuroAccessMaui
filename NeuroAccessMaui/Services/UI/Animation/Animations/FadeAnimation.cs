using System.Globalization;
using NeuroAccessMaui.Services.UI.Extensions;
using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Animations;

public class FadeToAnimation : AnimationBase
{
	public static readonly BindableProperty OpacityProperty =
		BindableProperty.Create(nameof(Opacity), typeof(double), typeof(FadeToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Opacity
	{
		get { return (double)this.GetValue(OpacityProperty); }
		set { this.SetValue(OpacityProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.FadeTo(this.Opacity,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}

public class FadeInAnimation : AnimationBase
{
	public enum FadeDirection
	{
		Up,
		Down
	}

	public static readonly BindableProperty DirectionProperty =
		BindableProperty.Create(nameof(Direction), typeof(FadeDirection), typeof(FadeInAnimation), FadeDirection.Up,
			BindingMode.TwoWay, null);

	public FadeDirection Direction
	{
		get { return (FadeDirection)this.GetValue(DirectionProperty); }
		set { this.SetValue(DirectionProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				this.Target.Animate("FadeIn", this.FadeIn(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation FadeIn()
	{
		Animation Animation = [];

		Animation.WithConcurrent((f) => this.Target.Opacity = f, 0, 1, Microsoft.Maui.Easing.CubicOut);
		Animation.WithConcurrent((f) => this.Target.TranslationY = f,
			this.Target.TranslationY + ((this.Direction == FadeDirection.Up) ? 50 : -50),
			this.Target.TranslationY, Microsoft.Maui.Easing.CubicOut, 0, 1);

		return Animation;
	}
}

public class FadeOutAnimation : AnimationBase
{
	public enum FadeDirection
	{
		Up,
		Down
	}

	public static readonly BindableProperty DirectionProperty =
		BindableProperty.Create(nameof(Direction), typeof(FadeDirection), typeof(FadeOutAnimation), FadeDirection.Up,
			BindingMode.TwoWay, null);

	public FadeDirection Direction
	{
		get { return (FadeDirection)this.GetValue(DirectionProperty); }
		set { this.SetValue(DirectionProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				this.Target.Animate("FadeOut", this.FadeOut(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation FadeOut()
	{
		Animation Animation = [];

		Animation.WithConcurrent((f) => this.Target.Opacity = f, 1, 0);
		Animation.WithConcurrent((f) => this.Target.TranslationY = f, this.Target.TranslationY, this.Target.TranslationY + ((this.Direction == FadeDirection.Up) ? 50 : -50));

		return Animation;
	}
}
