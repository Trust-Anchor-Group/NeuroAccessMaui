using NeuroAccessMaui.Services.UI.Animations;

namespace NeuroAccessMaui.Services.UI.Helpers;

public static class EasingHelper
{
	public static Easing GetEasing(EasingType Type)
	{
		return Type switch
		{
			EasingType.BounceIn => Easing.BounceIn,
			EasingType.BounceOut => Easing.BounceOut,
			EasingType.CubicIn => Easing.CubicIn,
			EasingType.CubicInOut => Easing.CubicInOut,
			EasingType.CubicOut => Easing.CubicOut,
			EasingType.Linear => Easing.Linear,
			EasingType.SinIn => Easing.SinIn,
			EasingType.SinInOut => Easing.SinInOut,
			EasingType.SinOut => Easing.SinOut,
			EasingType.SpringIn => Easing.SpringIn,
			EasingType.SpringOut => Easing.SpringOut,
			_ => throw new NotImplementedException(),
		};
	}
}
