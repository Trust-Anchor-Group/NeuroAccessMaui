using System.Globalization;
using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Animations;

public class TranslateToAnimation : AnimationBase
{
	public static readonly BindableProperty TranslateXProperty =
		BindableProperty.Create(nameof(TranslateX), typeof(double), typeof(TranslateToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double TranslateX
	{
		get { return (double)this.GetValue(TranslateXProperty); }
		set { this.SetValue(TranslateXProperty, value); }
	}

	public static readonly BindableProperty TranslateYProperty =
		BindableProperty.Create(nameof(TranslateY), typeof(double), typeof(TranslateToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double TranslateY
	{
		get { return (double)this.GetValue(TranslateYProperty); }
		set { this.SetValue(TranslateYProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.TranslateTo(this.TranslateX, this.TranslateY,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}
