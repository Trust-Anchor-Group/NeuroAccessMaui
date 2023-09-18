using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class HeartAnimation : AnimationBase
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
				this.Target.Animate("Hearth", this.Hearth(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation Hearth()
	{
		Animation Animation = new();

		Animation.WithConcurrent((f) => this.Target.Scale = f, this.Target.Scale, this.Target.Scale, Microsoft.Maui.Easing.Linear, 0, 0.1);
		Animation.WithConcurrent((f) => this.Target.Scale = f, this.Target.Scale, this.Target.Scale * 1.1, Microsoft.Maui.Easing.Linear, 0.1, 0.4);
		Animation.WithConcurrent((f) => this.Target.Scale = f, this.Target.Scale * 1.1, this.Target.Scale, Microsoft.Maui.Easing.Linear, 0.4, 0.5);
		Animation.WithConcurrent((f) => this.Target.Scale = f, this.Target.Scale, this.Target.Scale * 1.1, Microsoft.Maui.Easing.Linear, 0.5, 0.8);
		Animation.WithConcurrent((f) => this.Target.Scale = f, this.Target.Scale * 1.1, this.Target.Scale, Microsoft.Maui.Easing.Linear, 0.8, 1);

		return Animation;
	}
}
