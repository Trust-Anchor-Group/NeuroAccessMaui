using SkiaSharp;
using Waher.Content.Images;
using Waher.Content.QR;
using Waher.Script.Graphs.Functions.Colors;

namespace NeuroAccessMaui.Services.UI.QR;

/// <summary>
/// Performs custom coloring of QR codes
/// </summary>
public class CustomColoring
{
	private readonly SKBitmap icon;
	private readonly uint codeFg;
	private readonly uint codeBg;
	private readonly uint codeBgFg25;
	private readonly uint codeBgFg50;
	private readonly uint codeBgFg75;
	private readonly uint markerOuterFg;
	private readonly uint markerInnerFg;
	private readonly uint markerBg;
	private readonly uint alignmentOuterFg;
	private readonly uint alignmentInnerFg;
	private readonly uint alignmentBg;
	private readonly bool hasIcon;

	/// <summary>
	/// Performs custom coloring of QR codes
	/// </summary>
	/// <param name="SvgIconPath">Optional Icon to overlay the QR code. Must be in SVG Path format.</param>
	/// <param name="IconWidth">Width of icon.</param>
	/// <param name="IconHeight">Height of icon.</param>
	/// <param name="IconFg">Icon foreground color.</param>
	/// <param name="IconBg">Icon background color.</param>
	/// <param name="CodeFg">Code foreground color.</param>
	/// <param name="CodeBg">Code background color.</param>
	/// <param name="MarkerOuterFg">Outer marker foreground color.</param>
	/// <param name="MarkerInnerFg">Inner marker foreground color.</param>
	/// <param name="MarkerBg">Marker background color.</param>
	/// <param name="AlignmentOuterFg">Outer alignment foreground color.</param>
	/// <param name="AlignmentInnerFg">Inner alignment foreground color.</param>
	/// <param name="AlignmentBg">Alignment background color.</param>
	public CustomColoring(string SvgIconPath, int IconWidth, int IconHeight,
		SKColor IconFg, SKColor IconBg, SKColor CodeFg, SKColor CodeBg,
		SKColor MarkerOuterFg, SKColor MarkerInnerFg, SKColor MarkerBg,
		SKColor AlignmentOuterFg, SKColor AlignmentInnerFg, SKColor AlignmentBg)
	{
		if (string.IsNullOrEmpty(SvgIconPath))
		{
			this.icon = null;
			this.hasIcon = false;
		}
		else
		{
			this.icon = SvgPath.SvgPathToBitmap(SvgIconPath, 256, 256, IconFg, IconBg,
				0, 0, 256f / IconWidth, 256f / IconHeight);

			this.hasIcon = true;
		}

		this.codeFg = ToUInt(CodeFg);
		this.codeBg = ToUInt(CodeBg);
		this.codeBgFg25 = ToUInt(Blend.BlendColors(CodeBg, CodeFg, 0.25));
		this.codeBgFg50 = ToUInt(Blend.BlendColors(CodeBg, CodeFg, 0.50));
		this.codeBgFg75 = ToUInt(Blend.BlendColors(CodeBg, CodeFg, 0.75));
		this.markerOuterFg = ToUInt(MarkerOuterFg);
		this.markerInnerFg = ToUInt(MarkerInnerFg);
		this.markerBg = ToUInt(MarkerBg);
		this.alignmentOuterFg = ToUInt(AlignmentOuterFg);
		this.alignmentInnerFg = ToUInt(AlignmentInnerFg);
		this.alignmentBg = ToUInt(AlignmentBg);
	}

	private static uint ToUInt(SKColor Color)
	{
		uint Result = Color.Alpha;

		Result <<= 8;
		Result |= Color.Blue;
		Result <<= 8;
		Result |= Color.Green;
		Result <<= 8;
		Result |= Color.Red;

		return Result;
	}

	/// <summary>
	/// Color function called from the QR-code image generator.
	/// </summary>
	/// <param name="CodeX">Normalized [0-1] X-coordinate into QR-code.</param>
	/// <param name="CodeY">Normalized [0-1] Y-coordinate into QR-code.</param>
	/// <param name="DotX">Normalized [0-1] X-coordinate into current dot.</param>
	/// <param name="DotY">Normalized [0-1] Y-coordinate into current dot.</param>
	/// <param name="Type">Type of dot currently being drawn.</param>
	/// <returns>32-but unsigned integer representing the color of the current pixel, in AABBGGRR format
	/// (most significant byte first).</returns>
	public uint ColorFunction(float CodeX, float CodeY, float DotX, float DotY, DotType Type)
	{
		float dx, dy, d2;

		if (this.hasIcon)
		{
			dx = (CodeX - 0.5f);
			dy = (CodeY - 0.5f);
			d2 = dx * dx + dy * dy;

			if (d2 < 0.01)
			{
				int IconX = (int)(128 + 1024 * dx + 0.5f);
				int IconY = (int)(128 + 1024 * dy + 0.5f);

				if (IconX >= 0 && IconX < this.icon.Width &&
					IconY >= 0 && IconY < this.icon.Height)
				{
					return ToUInt(this.icon.GetPixel(IconX, IconY));
				}
			}

			if (d2 < 0.015)
			{
				return this.codeBg;
			}
		}

		switch (Type)
		{
			case DotType.CodeBackground:
			default:
				return this.codeBg;

			case DotType.CodeForeground:
				dx = (DotX - 0.5f);
				dy = (DotY - 0.5f);
				d2 = dx * dx + dy * dy;

				if (d2 <= 0.25f)
				{
					return this.codeFg;
				}
				else if (d2 <= 0.27f)
				{
					return this.codeBgFg75;
				}
				else if (d2 <= 0.29f)
				{
					return this.codeBgFg50;
				}
				else if (d2 <= 0.30f)
				{
					return this.codeBgFg25;
				}
				else
				{
					return this.codeBg;
				}

			case DotType.FinderMarkerBackground:
				return this.markerBg;

			case DotType.FinderMarkerForegroundOuter:
				return this.markerOuterFg;

			case DotType.FinderMarkerForegroundInner:
				return this.markerInnerFg;

			case DotType.AlignmentMarkerBackground:
				return this.alignmentBg;

			case DotType.AlignmentMarkerForegroundOuter:
				return this.alignmentOuterFg;

			case DotType.AlignmentMarkerForegroundInner:
				return this.alignmentInnerFg;
		};
	}
}
