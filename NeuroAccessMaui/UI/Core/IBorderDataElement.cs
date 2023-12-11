namespace NeuroAccessMaui.UI.Core
{
	internal interface IBorderDataElement
	{
		//note to implementor: implement this property publicly
		Style BorderStyle { get; }

		//note to implementor: but implement this method explicitly
		void OnBorderStylePropertyChanged(Style OldValue, Style NewValue);
	}
}
