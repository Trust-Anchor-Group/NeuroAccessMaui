using System.Windows.Input;

namespace NeuroAccessMaui.UI.Core;

internal interface ICommandElement
{
	// note to implementor: implement these properties publicly
	ICommand? Command { get; }
	object? CommandParameter { get; }

	// implement these explicitly
	void CanExecuteChanged(object? sender, EventArgs e);
}
