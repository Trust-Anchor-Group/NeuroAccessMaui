using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI;

public class GeometryConverter
{
	private static readonly PathGeometryConverter defaultPathGeometryConverter = new PathGeometryConverter();

	public static PathGeometry ParseStringToPathGeometry(string? PathString)
	{
		return defaultPathGeometryConverter.ConvertFrom(null, null, PathString) as PathGeometry ?? new();
	}
}
