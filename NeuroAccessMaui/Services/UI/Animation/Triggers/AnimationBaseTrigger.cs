using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Triggers;

public abstract class AnimationBaseTrigger<T> : TriggerAction<VisualElement>
{
	public T? From { get; set; } = default;
	public T? To { get; set; } = default;
	public uint Duration { get; set; } = 1000;
	public int Delay { get; set; } = 0;
	public EasingType Easing { get; set; } = EasingType.Linear;
	public BindableProperty? TargetProperty { get; set; } = default;

	protected override void Invoke(VisualElement Sender)
	{
		throw new NotImplementedException("Please Implement Invoke() in derived-class");
	}

	protected void SetDefaultFrom(T Property)
	{
		this.From = (this.From is null) || this.From.Equals(default(T)) ? Property : this.From;
	}
}
