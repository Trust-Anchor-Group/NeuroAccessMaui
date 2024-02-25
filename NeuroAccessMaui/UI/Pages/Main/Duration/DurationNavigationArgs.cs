using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Main.Duration
{
	/// <summary>
	/// Holds navigation parameters for the duration.
	/// </summary>
	/// <param name="Entry">Entry whose value is being calculated.</param>
	public class DurationNavigationArgs(Entry? Entry) : NavigationArgs
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
		public Entry? Entry { get; } = Entry;
	}
}
