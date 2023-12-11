namespace NeuroAccessMaui.UI.Core
{
	internal static class LabelDataElement
	{
		/// <summary>Bindable property for <see cref="ILabelDataElement.LabelData"/>.</summary>
		public static readonly BindableProperty LabelDataProperty =
			BindableProperty.Create(nameof(ILabelDataElement.LabelData), typeof(string), typeof(ILabelDataElement), default(string),
									propertyChanged: OnLabelDataPropertyChanged);

		static void OnLabelDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((ILabelDataElement)Bindable).OnLabelDataPropertyChanged((string)OldValue, (string)NewValue);
		}

		/// <summary>Bindable property for <see cref="ILabelDataElement.LabelStyle"/>.</summary>
		public static readonly BindableProperty LabelStyleProperty =
			BindableProperty.Create(nameof(ILabelDataElement.LabelStyle), typeof(Style), typeof(ILabelDataElement), default(Style),
									propertyChanged: OnLabelStylePropertyChanged);

		static void OnLabelStylePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((ILabelDataElement)Bindable).OnLabelStylePropertyChanged((Style)OldValue, (Style)NewValue);
		}
	}
}
