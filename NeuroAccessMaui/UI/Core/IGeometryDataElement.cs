using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Core;

public interface IGeometryDataElement
{
	//note to implementor: implement this property publicly
	Geometry GeometryData { get; }

	//note to implementor: but implement this method explicitly
	void OnGeometryDataPropertyChanged(Geometry OldValue, Geometry NewValue);
}
