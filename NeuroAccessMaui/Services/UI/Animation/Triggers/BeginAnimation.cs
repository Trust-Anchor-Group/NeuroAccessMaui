using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Triggers;

[ContentProperty("Animation")]
public class BeginAnimation : TriggerAction<VisualElement>
{
	public AnimationBase? Animation { get; set; }

	protected override async void Invoke(VisualElement Sender)
	{
		await (this.Animation?.Begin() ?? Task.CompletedTask);
	}
}
