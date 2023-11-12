namespace NeuroAccessMaui.UI.Core;

public interface IBorderDataElement
{
	//note to implementor: implement this property publicly
	IShape BorderStrokeShape { get; }
	Color BorderBackgroundColor { get; }
	Thickness BorderPadding { get; }

	//note to implementor: but implement this method explicitly
	void OnBorderStrokeShapePropertyChanged(IShape OldValue, IShape NewValue);
	void OnBorderBackgroundColorPropertyChanged(Color OldValue, Color NewValue);
	void OnBorderPaddingPropertyChanged(Thickness OldValue, Thickness NewValue);
	Thickness BorderPaddingDefaultValueCreator();
}
