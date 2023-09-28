using System.Globalization;
using CommunityToolkit.Maui.Markup;

namespace NeuroAccessMaui.Services.UI.Animations;

public class BlinkAnimation : AnimationBase
{
	protected override Task BeginAnimation()
	{
		if (this.Target is null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				this.Target.Animate("Blink", this.Blink(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation Blink()
	{
		Animation Animation = [];

		double MaxOpacity = 1;
		double MinOpacity = 0.8;

		Animation.WithConcurrent((f) => this.Target.Opacity = f, MaxOpacity, MinOpacity, Microsoft.Maui.Easing.CubicOut, 0, 0.5);
		Animation.WithConcurrent((f) => this.Target.Opacity = f, MinOpacity, MaxOpacity, Microsoft.Maui.Easing.CubicOut, 0.5, 1);

		return Animation;
	}
}
