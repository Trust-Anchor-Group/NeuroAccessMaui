using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Core;

static class GeometryDataElement
{
	/// <summary>Bindable property for <see cref="IGeometryDataElement.GeometryData"/>.</summary>
	public static readonly BindableProperty GeometryDataProperty =
		BindableProperty.Create(nameof(IGeometryDataElement.GeometryData), typeof(Geometry), typeof(IGeometryDataElement), default(Geometry),
								propertyChanged: OnGeometryDataPropertyChanged);

	static void OnGeometryDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IGeometryDataElement)Bindable).OnGeometryDataPropertyChanged((Geometry)OldValue, (Geometry)NewValue);
	}
}
