namespace NeuroAccessMaui.UI.Core;

internal interface ILabelDataElement
{
	//note to implementor: implement this property publicly
	string LabelData { get; }
	Style LabelStyle { get; }

	//note to implementor: but implement this method explicitly
	void OnLabelDataPropertyChanged(string OldValue, string NewValue);
	void OnLabelStylePropertyChanged(Style OldValue, Style NewValue);
}
