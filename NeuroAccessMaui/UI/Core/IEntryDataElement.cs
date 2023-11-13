namespace NeuroAccessMaui.UI.Core;

public interface IEntryDataElement
{
	//note to implementor: implement this property publicly
	string EntryData { get; }
	string EntryHint { get; }
	Style EntryStyle { get; }

	//note to implementor: but implement this method explicitly
	void OnEntryDataPropertyChanged(string OldValue, string NewValue);
	void OnEntryHintPropertyChanged(string OldValue, string NewValue);
	void OnEntryStylePropertyChanged(Style OldValue, Style NewValue);
}
