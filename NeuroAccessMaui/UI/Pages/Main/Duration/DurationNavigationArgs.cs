using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Main.Duration
{
	/// <summary>
	/// Holds navigation parameters for the duration.
	/// </summary>
	/// <param name="Entry">Entry whose value is being calculated.</param>
	public class DurationNavigationArgs(CompositeEntry? Entry) : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for the duration.
		/// </summary>
		public DurationNavigationArgs()
			: this(null)
		{
		}

		/// <summary>
		/// Entry whose value is being calculated.
		/// </summary>
		public CompositeEntry? Entry { get; } = Entry;
	}
}
