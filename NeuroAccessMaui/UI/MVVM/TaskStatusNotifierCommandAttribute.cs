using System;

namespace NeuroAccessMaui.UI.MVVM
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class TaskStatusNotifierCommandAttribute(
		TaskStatusNotifierCommandOptions options = TaskStatusNotifierCommandOptions.None)
		: Attribute
	{
		/// <summary>
		/// Gets the options to configure command behavior.
		/// </summary>
		public TaskStatusNotifierCommandOptions Options { get; } = options;
	}
}
