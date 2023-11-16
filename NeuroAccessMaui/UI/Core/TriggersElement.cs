namespace NeuroAccessMaui.UI.Core;

internal static class TriggersElement
{
	/// <summary>Bindable property for <see cref="ITriggersElement.TriggersData"/>.</summary>
	public static readonly BindableProperty TriggerDataProperty =
		BindableProperty.Create(nameof(ITriggersElement.TriggersData), typeof(IList<Trigger>), typeof(ITriggersElement), default(IList<Trigger>),
								propertyChanged: OnTriggersDataPropertyChanged);

	static void OnTriggersDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((ITriggersElement)Bindable).OnTriggersDataPropertyChanged((IList<Trigger>)OldValue, (IList<Trigger>)NewValue);
	}
}
