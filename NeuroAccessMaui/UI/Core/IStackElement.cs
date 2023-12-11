namespace NeuroAccessMaui.UI.Core
{
	internal interface IStackElement
	{
		//note to implementor: implement this property publicly
		double StackSpacing { get; }

		//note to implementor: but implement this method explicitly
		void OnStackSpacingPropertyChanged(double OldValue, double NewValue);
	}
}
