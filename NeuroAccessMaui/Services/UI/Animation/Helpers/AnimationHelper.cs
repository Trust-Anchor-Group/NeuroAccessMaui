namespace NeuroAccessMaui.Services.UI.Helpers;

public static class AnimationHelper
{
	public static int GetIntValue(int From, int To, double AnimationProgress)
	{
		return (int)(From + (To - From) * AnimationProgress);
	}

	public static double GetDoubleValue(double From, double To, double AnimationProgress)
	{
		return From + (To - From) * AnimationProgress;
	}

	// https://www.alanzucconi.com/2016/01/06/colour-interpolation/
	public static Color GetColorValue(Color? From, Color? To, double AnimationProgress)
	{
		if ((From is null) || (To is null))
		{
			return Color.FromInt(0);
		}

		double newR = (To.Red - From.Red) * AnimationProgress;
		double newG = (To.Green - From.Green) * AnimationProgress;
		double newB = (To.Blue - From.Blue) * AnimationProgress;

		return Color.FromRgb(From.Red + newR, From.Green + newG, From.Blue + newB);
	}

	public static CornerRadius GetCornerRadiusValue(CornerRadius From, CornerRadius To, double AnimationProgress)
	{
		return new CornerRadius(
			From.TopLeft + (To.TopLeft - From.TopLeft) * AnimationProgress,
			From.TopRight + (To.TopRight - From.TopRight) * AnimationProgress,
			From.BottomLeft + (To.BottomLeft - From.BottomLeft) * AnimationProgress,
			From.BottomRight + (To.BottomRight - From.BottomRight) * AnimationProgress);
	}

	public static Thickness GetThicknessValue(Thickness From, Thickness To, double AnimationProgress)
	{
		return new Thickness(
			From.Left + (To.Left - From.Left) * AnimationProgress,
			From.Top + (To.Top - From.Top) * AnimationProgress,
			From.Right + (To.Right - From.Right) * AnimationProgress,
			From.Bottom + (To.Bottom - From.Bottom) * AnimationProgress);
	}
}
