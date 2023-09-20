namespace NeuroAccessMaui.Services.UI.Behaviors;

public class AnimateProgressThickness : AnimationProgressBaseBehavior
{
	public static readonly BindableProperty FromProperty =
		BindableProperty.Create(nameof(From), typeof(Thickness), typeof(AnimationProgressBaseBehavior), default(Thickness),
			BindingMode.TwoWay, null);

	public Thickness From
	{
		get { return (Thickness)this.GetValue(FromProperty); }
		set { this.SetValue(FromProperty, value); }
	}

	public static readonly BindableProperty ToProperty =
		BindableProperty.Create(nameof(To), typeof(Thickness), typeof(AnimationProgressBaseBehavior), default(Thickness),
			BindingMode.TwoWay, null);

	public Thickness To
	{
		get { return (Thickness)this.GetValue(ToProperty); }
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

			double Left = (Value * (this.To.Left - this.From.Left) / Range) + this.From.Left;
			double Top = (Value * (this.To.Top - this.From.Top) / Range) + this.From.Top;
			double Right = (Value * (this.To.Right - this.From.Right) / Range) + this.From.Right;
			double Bottom = (Value * (this.To.Bottom - this.From.Bottom) / Range) + this.From.Bottom;

			Thickness TargetValue = new(Left, Top, Right, Bottom);

			this.Target.SetValue(this.TargetProperty, TargetValue);
		}
	}
}
