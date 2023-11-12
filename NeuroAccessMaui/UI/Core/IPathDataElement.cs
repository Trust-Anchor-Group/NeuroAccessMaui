namespace NeuroAccessMaui.UI.Core;

public interface IPathDataElement
{
	//note to implementor: implement this property publicly
	Brush PathFill { get; }
	Brush PathStroke { get; }

	//note to implementor: but implement this method explicitly
	void OnPathFillPropertyChanged(Brush OldValue, Brush NewValue);
	void OnPathStrokePropertyChanged(Brush OldValue, Brush NewValue);
}
