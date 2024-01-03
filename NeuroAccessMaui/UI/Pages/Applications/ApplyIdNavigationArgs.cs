using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// Navigation arguments for the <see cref="ApplyIdPage"/> and <see cref="ApplyIdViewModel"/>.
	/// </summary>
	/// <param name="Personal">If the ID application is a personal ID application.</param>
	public class ApplyIdNavigationArgs(bool Personal) : NavigationArgs()
	{
		/// <summary>
		/// Navigation arguments for the <see cref="ApplyIdPage"/> and <see cref="ApplyIdViewModel"/>.
		/// </summary>
		public ApplyIdNavigationArgs()
			: this(true)
		{
		}

		/// <summary>
		/// If it is a personal ID application.
		/// </summary>
		public bool Personal { get; } = Personal;

		/// <summary>
		/// If it is an organizational ID application.
		/// </summary>
		public bool Organizational { get; } = !Personal;
	}
}
