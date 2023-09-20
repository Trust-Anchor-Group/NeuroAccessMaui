using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Triggers;

public class AnimateThickness : AnimationBaseTrigger<Thickness>
{
	protected override async void Invoke(VisualElement Sender)
	{
		if (this.TargetProperty is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		if (this.Delay > 0)
		{
			await Task.Delay(this.Delay);
		}

		this.SetDefaultFrom((double)Sender.GetValue(this.TargetProperty));

		Sender.Animate($"AnimateThickness{this.TargetProperty.PropertyName}", new Animation((Progress) =>
		{
			Sender.SetValue(this.TargetProperty, AnimationHelper.GetThicknessValue(this.From, this.To, Progress));
		}),
		length: this.Duration,
		easing: EasingHelper.GetEasing(this.Easing));
	}
}
