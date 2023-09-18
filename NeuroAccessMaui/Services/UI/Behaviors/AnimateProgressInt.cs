namespace NeuroAccessMaui.Services.UI.Behaviors;

public class AnimateProgressInt : AnimationProgressBaseBehavior
{
	public static readonly BindableProperty FromProperty =
		BindableProperty.Create(nameof(From), typeof(int), typeof(AnimationProgressBaseBehavior), default(int),
			BindingMode.TwoWay, null);

	public int From
	{
		get { return (int)this.GetValue(FromProperty); }
		set { this.SetValue(FromProperty, value); }
	}

	public static readonly BindableProperty ToProperty =
		BindableProperty.Create(nameof(To), typeof(int), typeof(AnimationProgressBaseBehavior), default(int),
			BindingMode.TwoWay, null);

	public int To
	{
		get { return (int)this.GetValue(ToProperty); }
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

			int TargetValue = (int)((Value * (this.To - this.From) / Range) + this.From);

			this.Target.SetValue(this.TargetProperty, TargetValue);
		}
	}
}
