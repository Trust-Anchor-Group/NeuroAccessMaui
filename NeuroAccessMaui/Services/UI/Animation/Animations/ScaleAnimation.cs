using System.Globalization;
using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Animations;

public class ScaleToAnimation : AnimationBase
{
	public static readonly BindableProperty ScaleProperty =
		BindableProperty.Create(nameof(Scale), typeof(double), typeof(ScaleToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Scale
	{
		get { return (double)this.GetValue(ScaleProperty); }
		set { this.SetValue(ScaleProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.ScaleTo(this.Scale,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}

public class RelScaleToAnimation : AnimationBase
{
	public static readonly BindableProperty ScaleProperty =
		BindableProperty.Create(nameof(Scale), typeof(double), typeof(RelScaleToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Scale
	{
		get { return (double)this.GetValue(ScaleProperty); }
		set { this.SetValue(ScaleProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.RelScaleTo(this.Scale,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}
