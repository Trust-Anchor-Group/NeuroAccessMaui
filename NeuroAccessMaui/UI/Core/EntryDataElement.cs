namespace NeuroAccessMaui.UI.Core;

static class EntryDataElement
{
	/// <summary>Bindable property for <see cref="IEntryDataElement.EntryData"/>.</summary>
	public static readonly BindableProperty EntryDataProperty =
		BindableProperty.Create(nameof(IEntryDataElement.EntryData), typeof(string), typeof(IEntryDataElement), default(string),
								defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnEntryDataPropertyChanged);

	static void OnEntryDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IEntryDataElement)Bindable).OnEntryDataPropertyChanged((string)OldValue, (string)NewValue);
	}

	/// <summary>Bindable property for <see cref="IEntryDataElement.EntryHint"/>.</summary>
	public static readonly BindableProperty EntryHintProperty =
		BindableProperty.Create(nameof(IEntryDataElement.EntryHint), typeof(string), typeof(IEntryDataElement), default(string),
								propertyChanged: OnEntryHintPropertyChanged);

	static void OnEntryHintPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IEntryDataElement)Bindable).OnEntryHintPropertyChanged((string)OldValue, (string)NewValue);
	}

	/// <summary>Bindable property for <see cref="IEntryDataElement.EntryStyle"/>.</summary>
	public static readonly BindableProperty EntryStyleProperty =
		BindableProperty.Create(nameof(IEntryDataElement.EntryStyle), typeof(Style), typeof(IEntryDataElement), default(Style),
								propertyChanged: OnEntryStylePropertyChanged);

	static void OnEntryStylePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IEntryDataElement)Bindable).OnEntryStylePropertyChanged((Style)OldValue, (Style)NewValue);
	}
}
