﻿using System.Globalization;

namespace NeuroAccessMaui.Services.UI.Animations;

public class ShakeAnimation : AnimationBase
{
	private const int Movement = 5;

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
				Target.Animate("Shake", Shake(), 16, Convert.ToUInt32(Duration, CultureInfo.InvariantCulture));
			});
		});
	}

	internal Animation Shake()
	{
		var animation = new Animation();

		animation.WithConcurrent(
			(f) => Target.TranslationX = f,
			Target.TranslationX + Movement, Target.TranslationX,
			Microsoft.Maui.Easing.Linear, 0, 0.1);

		animation.WithConcurrent(
			(f) => Target.TranslationX = f,
			Target.TranslationX - Movement, Target.TranslationX,
			Microsoft.Maui.Easing.Linear, 0.1, 0.2);

		animation.WithConcurrent(
			(f) => Target.TranslationX = f,
			Target.TranslationX + Movement, Target.TranslationX,
			Microsoft.Maui.Easing.Linear, 0.2, 0.3);

		animation.WithConcurrent(
			(f) => Target.TranslationX = f,
			Target.TranslationX - Movement, Target.TranslationX,
			Microsoft.Maui.Easing.Linear, 0.3, 0.4);

		animation.WithConcurrent(
			 (f) => Target.TranslationX = f,
			 Target.TranslationX + Movement, Target.TranslationX,
			 Microsoft.Maui.Easing.Linear, 0.4, 0.5);

		animation.WithConcurrent(
			(f) => Target.TranslationX = f,
			Target.TranslationX - Movement, Target.TranslationX,
			Microsoft.Maui.Easing.Linear, 0.5, 0.6);

		animation.WithConcurrent(
			 (f) => Target.TranslationX = f,
			 Target.TranslationX + Movement, Target.TranslationX,
			 Microsoft.Maui.Easing.Linear, 0.6, 0.7);

		animation.WithConcurrent(
			(f) => Target.TranslationX = f,
			Target.TranslationX - Movement, Target.TranslationX,
			Microsoft.Maui.Easing.Linear, 0.7, 0.8);

		return animation;
	}
}
