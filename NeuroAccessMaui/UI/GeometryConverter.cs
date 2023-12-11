using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI
{
	public class GeometryConverter
	{
		private static readonly PathGeometryConverter defaultPathGeometryConverter = new();

		public static PathGeometry ParseStringToPathGeometry(string? PathString, FillRule? FillRule = null)
		{
			if (defaultPathGeometryConverter.ConvertFrom(null, null, PathString) is PathGeometry Result)
			{
				if (FillRule is not null)
					Result.FillRule = (FillRule)FillRule;

				return Result;
			}

			return new();
		}
	}
}
