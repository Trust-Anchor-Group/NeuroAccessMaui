namespace NeuroAccessMaui.Services.UI.Behaviors;

public class AnimateProgressColor : AnimationProgressBaseBehavior
{
	public static readonly BindableProperty FromProperty =
		  BindableProperty.Create(nameof(From), typeof(Color), typeof(AnimationProgressBaseBehavior), default(Color),
			  BindingMode.TwoWay, null);

	public Color From
	{
		get { return (Color)this.GetValue(FromProperty); }
		set { this.SetValue(FromProperty, value); }
	}

	public static readonly BindableProperty ToProperty =
		BindableProperty.Create(nameof(To), typeof(Color), typeof(AnimationProgressBaseBehavior), default(Color),
			BindingMode.TwoWay, null);

	public Color To
	{
		get { return (Color)this.GetValue(ToProperty); }
		set { this.SetValue(ToProperty, value); }
	}

	protected override void OnUpdate()
	{
		if ((this.Target is not null) && this.Progress.HasValue)
		{
			if ((this.From is null) && (this.To is null))
			{
				return;
			}

			if (this.From == null)
			{
				this.Target.SetValue(this.TargetProperty, this.To);
				return;
			}

			if (this.To == null)
			{
				this.Target.SetValue(this.TargetProperty, this.From);
				return;
			}

			double Value = this.Progress.Value;
			double NewR = (this.To.Red - this.From.Red) * Value;
			double NewG = (this.To.Green - this.From.Green) * Value;
			double NewB = (this.To.Blue - this.From.Blue) * Value;

			Color TargetValue = Color.FromRgb((int)(this.From.Red + NewR), (int)(this.From.Green + NewG), (int)(this.From.Blue + NewB));

			this.Target.SetValue(this.TargetProperty, TargetValue);
		}
	}
}
