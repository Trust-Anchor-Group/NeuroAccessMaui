using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class BounceInAnimation : AnimationBase
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
				this.Target.Animate("BounceIn", this.BounceIn(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation BounceIn()
	{
		Animation Animation = [];

		Animation.WithConcurrent((f) => this.Target.Scale = f, 0.5, 1, Microsoft.Maui.Easing.Linear, 0, 1);
		Animation.WithConcurrent((f) => this.Target.Opacity = f, 0, 1, null, 0, 0.25);

		return Animation;
	}
}

public class BounceOutAnimation : AnimationBase
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
				this.Target.Animate("BounceOut", this.BounceOut(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation BounceOut()
	{
		Animation animation = [];

		this.Target.Opacity = 1;

		animation.WithConcurrent((f) => this.Target.Opacity = f, 1, 0, null, 0.5, 1);
		animation.WithConcurrent((f) => this.Target.Scale = f, 1, 0.3, Microsoft.Maui.Easing.Linear, 0, 1);

		return animation;
	}
}
