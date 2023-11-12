using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Core;

static class PathDataElement
{
	/// <summary>Bindable property for <see cref="IPathDataElement.GeometryData"/>.</summary>
	public static readonly BindableProperty GeometryDataProperty =
		BindableProperty.Create(nameof(IPathDataElement.GeometryData), typeof(Geometry), typeof(IPathDataElement), default(Geometry),
								propertyChanged: OnGeometryDataPropertyChanged);

	static void OnGeometryDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IPathDataElement)Bindable).OnGeometryDataPropertyChanged((Geometry)OldValue, (Geometry)NewValue);
	}

	/// <summary>Bindable property for <see cref="IPathDataElement.PathStyle"/>.</summary>
	public static readonly BindableProperty PathStyleProperty =
		BindableProperty.Create(nameof(IPathDataElement.PathStyle), typeof(Style), typeof(IPathDataElement), default(Style),
								propertyChanged: OnPathStylePropertyChanged);

	static void OnPathStylePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IPathDataElement)Bindable).OnPathStylePropertyChanged((Style)OldValue, (Style)NewValue);
	}
}
