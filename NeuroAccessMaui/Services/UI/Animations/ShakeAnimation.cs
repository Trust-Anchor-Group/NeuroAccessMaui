using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class ShakeAnimation : AnimationBase
{
	private const int movement = 5;

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
				this.Target.Animate("Shake", this.Shake(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation Shake()
	{
		Animation Animation = [];

		double TranslationXPlus = this.Target.TranslationX + movement;
		double TranslationXMinus = this.Target.TranslationX - movement;

		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXPlus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0, 0.1);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXMinus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.1, 0.2);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXPlus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.2, 0.3);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXMinus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.3, 0.4);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXPlus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.4, 0.5);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXMinus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.5, 0.6);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXPlus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.6, 0.7);
		Animation.WithConcurrent((f) => this.Target.TranslationX = f, TranslationXMinus, this.Target.TranslationX, Microsoft.Maui.Easing.Linear, 0.7, 0.8);

		return Animation;
	}
}
