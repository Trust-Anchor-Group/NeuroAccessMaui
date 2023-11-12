namespace NeuroAccessMaui.UI.Core;

public interface IBorderDataElement
{
	//note to implementor: implement this property publicly
	Style BorderStyle { get; }
	IShape BorderStrokeShape { get; }
	Color BorderBackgroundColor { get; }
	Thickness BorderPadding { get; }

	//note to implementor: but implement this method explicitly
	void OnBorderStylePropertyChanged(Style OldValue, Style NewValue);
	void OnBorderStrokeShapePropertyChanged(IShape OldValue, IShape NewValue);
	void OnBorderBackgroundColorPropertyChanged(Color OldValue, Color NewValue);
	void OnBorderPaddingPropertyChanged(Thickness OldValue, Thickness NewValue);
	Thickness BorderPaddingDefaultValueCreator();
}
