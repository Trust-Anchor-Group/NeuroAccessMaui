using NeuroAccessMaui.Services.UI.Helpers;

namespace NeuroAccessMaui.Services.UI.Triggers;

public class AnimateCornerRadius : AnimationBaseTrigger<CornerRadius>
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

		this.SetDefaultFrom((CornerRadius)Sender.GetValue(this.TargetProperty));

		Sender.Animate($"AnimateCornerRadius{this.TargetProperty.PropertyName}", new Animation((Progress) =>
		{
			Sender.SetValue(this.TargetProperty, AnimationHelper.GetCornerRadiusValue(this.From, this.To, Progress));
		}),
		length: this.Duration,
		easing: EasingHelper.GetEasing(this.Easing));
	}
}
