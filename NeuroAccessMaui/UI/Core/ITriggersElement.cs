namespace NeuroAccessMaui.UI.Core;

internal interface ITriggersElement
{
	// note to implementor: implement these properties publicly
	IList<Trigger> TriggersData { get; }

	//note to implementor: but implement this method explicitly
	void OnTriggersDataPropertyChanged(IList<Trigger> OldValue, IList<Trigger> NewValue);
}
