namespace NeuroAccessMaui.Services.UI.Extensions;

public static class ColorExtensions
{
	public static Task<bool> ColorTo(this VisualElement Self, Color FromColor, Color ToColor, Action<Color> Callback, uint Length = 250, Easing? Easing = null)
	{
		Color Transform(double t) =>
			Color.FromRgba(FromColor.Red + t * (ToColor.Red - FromColor.Red),
						   FromColor.Green + t * (ToColor.Green - FromColor.Green),
						   FromColor.Blue + t * (ToColor.Blue - FromColor.Blue),
						   FromColor.Alpha + t * (ToColor.Alpha - FromColor.Alpha));

		return ColorAnimation(Self, "ColorTo", Transform, Callback, Length, Easing);
	}

	public static void CancelAnimation(this VisualElement Self)
	{
		Self.AbortAnimation("ColorTo");
	}

	static Task<bool> ColorAnimation(VisualElement Element, string Name, Func<double, Color> Transform, Action<Color> Callback, uint Length, Easing? Easing)
	{
		Easing ??= Easing.Linear;
		TaskCompletionSource<bool> TaskCompletionSource = new();

		Element.Animate<Color>(Name, Transform, Callback, 16, Length, Easing, (v, c) => TaskCompletionSource.SetResult(c));

		return TaskCompletionSource.Task;
	}
}
