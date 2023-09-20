namespace NeuroAccessMaui.Services.UI.Animations;

[ContentProperty("Animations")]
public class StoryBoard : AnimationBase
{
	public StoryBoard()
	{
		this.Animations = [];
	}

	public StoryBoard(List<AnimationBase> Animations)
	{
		this.Animations = Animations;
	}

	public List<AnimationBase> Animations
	{
		get;
	}

	protected override async Task BeginAnimation()
	{
		foreach (AnimationBase Animation in this.Animations)
		{
			Animation.Target ??= this.Target;
			await Animation.Begin();
		}
	}
}
