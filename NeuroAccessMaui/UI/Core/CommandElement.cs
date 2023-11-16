using System.Windows.Input;

namespace NeuroAccessMaui.UI.Core;

internal static class CommandElement
{
	public static void OnCommandChanging(BindableObject bo, object o, object n)
	{
		ICommandElement CommandElement = (ICommandElement)bo;

		if (o is ICommand OldCommand)
		{
			OldCommand.CanExecuteChanged -= CommandElement.CanExecuteChanged;
		}
	}

	public static void OnCommandChanged(BindableObject bo, object o, object n)
	{
		ICommandElement CommandElement = (ICommandElement)bo;

		if (n is ICommand NewCommand)
		{
			NewCommand.CanExecuteChanged += CommandElement.CanExecuteChanged;
		}

		CommandElement.CanExecuteChanged(bo, EventArgs.Empty);
	}

	public static void OnCommandParameterChanged(BindableObject bo, object o, object n)
	{
		ICommandElement CommandElement = (ICommandElement)bo;

		CommandElement.CanExecuteChanged(bo, EventArgs.Empty);
	}

	public static bool GetCanExecute(ICommandElement CommandElement)
	{
		if (CommandElement.Command == null)
		{
			return true;
		}

		return CommandElement.Command.CanExecute(CommandElement.CommandParameter);
	}
}
