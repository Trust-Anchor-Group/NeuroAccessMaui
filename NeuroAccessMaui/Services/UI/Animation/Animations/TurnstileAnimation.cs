using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class TurnstileInAnimation : AnimationBase
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
				this.Target.Animate("TurnstileIn", this.TurnstileIn(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation TurnstileIn()
	{
		Animation Animation = [];

		Animation.WithConcurrent((f) => this.Target.RotationY = f, 75, 0, Microsoft.Maui.Easing.CubicOut);
		Animation.WithConcurrent((f) => this.Target.Opacity = f, 0, 1, null, 0, 0.01);

		return Animation;
	}
}

public class TurnstileOutAnimation : AnimationBase
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
				this.Target.Animate("TurnstileOut", this.TurnstileOut(), 16,
					Convert.ToUInt32(this.Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation TurnstileOut()
	{
		Animation Animation = [];

		Animation.WithConcurrent((f) => this.Target.RotationY = f, 0, -75, Microsoft.Maui.Easing.CubicOut);
		Animation.WithConcurrent((f) => this.Target.Opacity = f, 1, 0, null, 0.9, 1);

		return Animation;
	}
}
