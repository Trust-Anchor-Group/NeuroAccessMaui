using System.Windows.Input;

namespace NeuroAccessMaui.UI.Core
{
	internal static class EntryDataElement
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

		/// <summary>Bindable property for <see cref="IEntryDataElement.ReturnCommand"/>.</summary>
		public static readonly BindableProperty ReturnCommandProperty =
			BindableProperty.Create(nameof(IEntryDataElement.ReturnCommand), typeof(ICommand), typeof(IEntryDataElement), default(ICommand),
				propertyChanged: OnReturnCommandPropertyChanged);

		static void OnReturnCommandPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((IEntryDataElement)Bindable).OnReturnCommandPropertyChanged((ICommand)OldValue, (ICommand)NewValue);
		}

		/// <summary>Bindable property for <see cref="IEntryDataElement.IsPassword"/>.</summary>
		public static readonly BindableProperty IsPasswordProperty =
			BindableProperty.Create(nameof(IEntryDataElement.IsPassword), typeof(bool), typeof(IEntryDataElement), default(bool),
				propertyChanged: OnIsPasswordPropertyChanged);

		static void OnIsPasswordPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((IEntryDataElement)Bindable).OnIsPasswordPropertyChanged((bool)OldValue, (bool)NewValue);
		}
	}
}
