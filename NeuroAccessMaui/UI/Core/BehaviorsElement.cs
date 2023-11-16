namespace NeuroAccessMaui.UI.Core;

internal static class BehaviorsElement
{
	/// <summary>Bindable property for <see cref="IBehaviorsElement.BehaviorsData"/>.</summary>
	public static readonly BindableProperty BehaviorsDataProperty =
		BindableProperty.Create(nameof(IBehaviorsElement.BehaviorsData), typeof(IList<Behavior>), typeof(IBehaviorsElement), default(IList<Behavior>),
								propertyChanged: OnBehaviorsDataPropertyChanged);

	static void OnBehaviorsDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IBehaviorsElement)Bindable).OnBehaviorsDataPropertyChanged((IList<Behavior>)OldValue, (IList<Behavior>)NewValue);
	}
}
