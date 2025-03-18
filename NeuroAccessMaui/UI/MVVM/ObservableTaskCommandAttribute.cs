using System;

namespace NeuroAccessMaui.UI.MVVM
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class ObservableTaskCommandAttribute(
		ObservableTaskCommandOptions options = ObservableTaskCommandOptions.None)
		: Attribute
	{
		/// <summary>
		/// Gets the options to configure command behavior.
		/// </summary>
		public ObservableTaskCommandOptions Options { get; } = options;
	}
}
