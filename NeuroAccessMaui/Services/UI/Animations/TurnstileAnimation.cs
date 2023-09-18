﻿using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class TurnstileInAnimation : AnimationBase
{
	protected override Task BeginAnimation()
	{
		if (Target == null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				Target.Animate("TurnstileIn", TurnstileIn(), 16, Convert.ToUInt32(Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation TurnstileIn()
	{
		var animation = new Animation();

		animation.WithConcurrent((f) => Target.RotationY = f, 75, 0, Microsoft.Maui.Easing.CubicOut);
		animation.WithConcurrent((f) => Target.Opacity = f, 0, 1, null, 0, 0.01);

		return animation;
	}
}

public class TurnstileOutAnimation : AnimationBase
{
	protected override Task BeginAnimation()
	{
		if (Target == null)
		{
			throw new NullReferenceException("Null Target property.");
		}

		return Task.Run(() =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				Target.Animate("TurnstileOut", TurnstileOut(), 16, Convert.ToUInt32(Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation TurnstileOut()
	{
		var animation = new Animation();

		animation.WithConcurrent((f) => Target.RotationY = f, 0, -75, Microsoft.Maui.Easing.CubicOut);
		animation.WithConcurrent((f) => Target.Opacity = f, 1, 0, null, 0.9, 1);

		return animation;
	}
}
