namespace NeuroAccessMaui.UI.Core
{
	internal interface IButtonElement : ICommandElement
	{
		//note to implementor: implement this property publicly
		//!!! not implemented yet
		// bool IsPressed { get; }


		//note to implementor: but implement these methods explicitly
		void PropagateUpClicked();
		//!!! not implemented yet
		// void PropagateUpPressed();
		// void PropagateUpReleased();
		// void SetIsPressed(bool isPressed);
	}
}
