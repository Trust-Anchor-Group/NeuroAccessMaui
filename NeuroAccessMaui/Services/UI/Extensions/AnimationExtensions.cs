using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Extensions;

public static class AnimationExtensions
{
	public static async Task<bool> Animate(this VisualElement Element, AnimationBase Animation)
	{
		try
		{
			Animation.Target = Element;

			await Animation.Begin();

			return true;
		}
		catch
		{
			return false;
		}
	}
}
