namespace NeuroAccessMaui.Services.UI.Behaviors;

public class AnimateProgressCornerRadius : AnimationProgressBaseBehavior
{
	public static readonly BindableProperty FromProperty =
		BindableProperty.Create(nameof(From), typeof(CornerRadius), typeof(AnimationProgressBaseBehavior), default(CornerRadius),
			BindingMode.TwoWay, null);

	public CornerRadius From
	{
		get { return (CornerRadius)this.GetValue(FromProperty); }
		set { this.SetValue(FromProperty, value); }
	}

	public static readonly BindableProperty ToProperty =
		BindableProperty.Create(nameof(To), typeof(CornerRadius), typeof(AnimationProgressBaseBehavior), default(CornerRadius),
			BindingMode.TwoWay, null);

	public CornerRadius To
	{
		get { return (CornerRadius)this.GetValue(ToProperty); }
		set { this.SetValue(ToProperty, value); }
	}

	protected override void OnUpdate()
	{
		if ((this.Target is not null) && this.Progress.HasValue)
		{
			if (this.Progress < this.Minimum)
			{
				this.Target.SetValue(this.TargetProperty, this.From);
				return;
			}

			if (this.Progress >= this.Maximum)
			{
				this.Target.SetValue(this.TargetProperty, this.To);
				return;
			}

			double Value = this.Progress.Value - this.Minimum;
			double Range = (this.Maximum - this.Minimum);

			double TopLeft = (Value * (this.To.TopLeft - this.From.TopLeft) / Range) + this.From.TopLeft;
			double TopRight = (Value * (this.To.TopRight - this.From.TopRight) / Range) + this.From.TopRight;
			double BottomLeft = (Value * (this.To.BottomLeft - this.From.BottomLeft) / Range) + this.From.BottomLeft;
			double BottomRight = (Value * (this.To.BottomRight - this.From.BottomRight) / Range) + this.From.BottomRight;

			CornerRadius TargetValue = new(TopLeft, TopRight, BottomLeft, BottomRight);

			this.Target.SetValue(this.TargetProperty, TargetValue);
		}
	}
}
