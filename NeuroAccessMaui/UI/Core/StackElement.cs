namespace NeuroAccessMaui.UI.Core
{
	internal static class StackElement
	{
		/// <summary>Bindable property for <see cref="IStackElement.StackSpacing"/>.</summary>
		public static readonly BindableProperty StackSpacingProperty =
			BindableProperty.Create(nameof(IStackElement.StackSpacing), typeof(double), typeof(IStackElement), default(double),
									propertyChanged: OnStackSpacingPropertyChanged);

		static void OnStackSpacingPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((IStackElement)Bindable).OnStackSpacingPropertyChanged((double)OldValue, (double)NewValue);
		}
	}
}
