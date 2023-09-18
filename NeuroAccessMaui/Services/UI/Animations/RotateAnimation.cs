using System.Globalization;
using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Animations;

public class RotateToAnimation : AnimationBase
{
	public static readonly BindableProperty RotationProperty =
		BindableProperty.Create(nameof(Rotation), typeof(double), typeof(RotateToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Rotation
	{
		get { return (double)this.GetValue(RotationProperty); }
		set { this.SetValue(RotationProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.RotateTo(this.Rotation,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}

public class RelRotateToAnimation : AnimationBase
{
	public static readonly BindableProperty RotationProperty =
		BindableProperty.Create(nameof(Rotation), typeof(double), typeof(RelRotateToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Rotation
	{
		get { return (double)this.GetValue(RotationProperty); }
		set { this.SetValue(RotationProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.RelRotateTo(this.Rotation,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}

public class RotateXToAnimation : AnimationBase
{
	public static readonly BindableProperty RotationProperty =
		BindableProperty.Create(nameof(Rotation), typeof(double), typeof(RotateXToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Rotation
	{
		get { return (double)this.GetValue(RotationProperty); }
		set { this.SetValue(RotationProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.RotateXTo(this.Rotation,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}

public class RotateYToAnimation : AnimationBase
{
	public static readonly BindableProperty RotationProperty =
		BindableProperty.Create(nameof(Rotation), typeof(double), typeof(RotateYToAnimation), default(double),
			BindingMode.TwoWay, null);

	public double Rotation
	{
		get { return (double)this.GetValue(RotationProperty); }
		set { this.SetValue(RotationProperty, value); }
	}

	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return this.Target.RotateYTo(this.Rotation,
			Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture),
			EasingHelper.GetEasing(this.Easing));
	}
}
