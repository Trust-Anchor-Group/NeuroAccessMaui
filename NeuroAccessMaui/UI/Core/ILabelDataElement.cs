namespace NeuroAccessMaui.UI.Core;

public interface ILabelDataElement
{
	//note to implementor: implement this property publicly
	string LabelData { get; }
	Style LabelStyle { get; }
	Color LabelTextColor { get; }
	LayoutOptions LabelHorizontalOptions { get; }
	LayoutOptions LabelVerticalOptions { get; }

	//note to implementor: but implement this method explicitly
	void OnLabelDataPropertyChanged(string OldValue, string NewValue);
	void OnLabelStylePropertyChanged(Style OldValue, Style NewValue);
	void OnLabelTextColorPropertyChanged(Color OldValue, Color NewValue);
	void OnLabelHorizontalOptionsPropertyChanged(LayoutOptions OldValue, LayoutOptions NewValue);
	void OnLabelVerticalOptionsPropertyChanged(LayoutOptions OldValue, LayoutOptions NewValue);
}
