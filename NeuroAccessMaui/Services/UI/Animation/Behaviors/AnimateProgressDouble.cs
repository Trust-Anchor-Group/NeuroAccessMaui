namespace NeuroAccessMaui.Services.UI.Behaviors;

public class AnimateProgressDouble : AnimationProgressBaseBehavior
{
	public static readonly BindableProperty FromProperty =
		BindableProperty.Create(nameof(From), typeof(double), typeof(AnimationProgressBaseBehavior), default(double),
			BindingMode.TwoWay, null);

	public double From
	{
		get { return (double)this.GetValue(FromProperty); }
		set { this.SetValue(FromProperty, value); }
	}

	public static readonly BindableProperty ToProperty =
		BindableProperty.Create(nameof(To), typeof(double), typeof(AnimationProgressBaseBehavior), default(double),
			BindingMode.TwoWay, null);

	public double To
	{
		get { return (double)this.GetValue(ToProperty); }
		set { this.SetValue(ToProperty, value); }
	}

	public static readonly BindableProperty MultiplyValueProperty =
		BindableProperty.Create(nameof(MultiplyValue), typeof(double), typeof(AnimateProgressDouble), 1.0d,
			BindingMode.TwoWay, null);

	public double MultiplyValue
	{
		get { return (double)this.GetValue(MultiplyValueProperty); }
		set { this.SetValue(MultiplyValueProperty, value); }
	}

	protected override void OnUpdate()
	{
		if ((this.Target is not null) && this.Progress.HasValue)
		{
			if (this.Progress < this.Minimum)
			{
				this.Target.SetValue(this.TargetProperty, this.From * this.MultiplyValue);
				return;
			}

			if (this.Progress >= this.Maximum)
			{
				this.Target.SetValue(this.TargetProperty, this.To * this.MultiplyValue);
				return;
			}

			double Value = this.Progress.Value - this.Minimum;
			double Range = (this.Maximum - this.Minimum);

			double TargetValue = (Value * (this.To - this.From) / Range) + this.From;

			this.Target.SetValue(this.TargetProperty, TargetValue * this.MultiplyValue);
		}
	}
}
