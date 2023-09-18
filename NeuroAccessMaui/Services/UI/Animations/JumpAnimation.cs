using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class JumpAnimation : AnimationBase
{
	private const int movement = -25;

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
				this.Target.Animate("Jump", this.Jump(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation Jump()
	{
		Animation Animation = new();

		Animation.WithConcurrent((f) => this.Target.TranslationY = f, this.Target.TranslationY, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0, 0.2);
		Animation.WithConcurrent((f) => this.Target.TranslationY = f, this.Target.TranslationY + movement, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.2, 0.4);
		Animation.WithConcurrent((f) => this.Target.TranslationY = f, this.Target.TranslationY, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.5, 1.0);

		return Animation;
	}
}
