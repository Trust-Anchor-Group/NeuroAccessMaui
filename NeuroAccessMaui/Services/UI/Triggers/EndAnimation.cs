using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Triggers;

public class EndAnimation : TriggerAction<VisualElement>
{
	public AnimationBase? Animation { get; set; }

	protected override void Invoke(VisualElement Sender)
	{
		this.Animation?.End();
	}
}
