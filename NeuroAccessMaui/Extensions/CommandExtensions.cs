using System.Windows.Input;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Helper/convenience methods for the <see cref="ICommand"/>.
	/// </summary>
	public static class CommandExtensions
	{
		/// <summary>
		/// Calls the <see cref="Command.ChangeCanExecute"/> method on the given <see cref="ICommand"/>, given that it <b>is</b> a <see cref="Command"/>.
		/// </summary>
		/// <param name="command"></param>
		public static void ChangeCanExecute(this ICommand command)
		{
			if (command is Command cmd)
			{
				cmd.ChangeCanExecute();
			}
		}
	}
}
