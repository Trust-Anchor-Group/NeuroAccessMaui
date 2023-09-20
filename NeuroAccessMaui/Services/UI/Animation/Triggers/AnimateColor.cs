using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Triggers;

public class AnimateColor : AnimationBaseTrigger<Color>
{
	protected override async void Invoke(VisualElement sender)
	{
		if (this.TargetProperty is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		if (this.Delay > 0)
		{
			await Task.Delay(this.Delay);
		}

		this.SetDefaultFrom((Color)sender.GetValue(this.TargetProperty));

		sender.Animate($"AnimateColor{this.TargetProperty.PropertyName}", new Animation((Progress) =>
		{
			sender.SetValue(this.TargetProperty, AnimationHelper.GetColorValue(this.From, this.To, Progress));
		}),
		length: this.Duration,
		easing: EasingHelper.GetEasing(this.Easing));
	}
}
