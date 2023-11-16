using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Core;

internal interface IPathDataElement
{
	//note to implementor: implement this property publicly
	Geometry PathData { get; }
	Style PathStyle { get; }

	//note to implementor: but implement this method explicitly
	void OnPathDataPropertyChanged(Geometry OldValue, Geometry NewValue);
	void OnPathStylePropertyChanged(Style OldValue, Style NewValue);
}
