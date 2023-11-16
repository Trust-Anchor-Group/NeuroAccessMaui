namespace NeuroAccessMaui.UI.Core;

internal interface IButtonElement : ICommandElement
{
	//note to implementor: implement this property publicly
	//!!!	bool IsPressed { get; }


	//note to implementor: but implement these methods explicitly
	void PropagateUpClicked();
	//!!!	void PropagateUpPressed();
	//!!!	void PropagateUpReleased();
	//!!!	void SetIsPressed(bool isPressed);
}
