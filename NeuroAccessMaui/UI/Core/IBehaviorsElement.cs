namespace NeuroAccessMaui.UI.Core;

internal interface IBehaviorsElement
{
	// note to implementor: implement these properties publicly
	IList<Behavior> BehaviorsData { get; }

	//note to implementor: but implement this method explicitly
	void OnBehaviorsDataPropertyChanged(IList<Behavior> OldValue, IList<Behavior> NewValue);
}
