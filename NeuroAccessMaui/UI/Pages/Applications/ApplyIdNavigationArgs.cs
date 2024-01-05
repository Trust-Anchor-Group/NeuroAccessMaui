using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// Navigation arguments for the <see cref="ApplyIdPage"/> and <see cref="ApplyIdViewModel"/>.
	/// </summary>
	/// <param name="Personal">If the ID application is a personal ID application.</param>
	/// <param name="ReusePhoto">If existing photo can be reused.</param>
	public class ApplyIdNavigationArgs(bool Personal, bool ReusePhoto) : NavigationArgs()
	{
		/// <summary>
		/// Navigation arguments for the <see cref="ApplyIdPage"/> and <see cref="ApplyIdViewModel"/>.
		/// </summary>
		public ApplyIdNavigationArgs()
			: this(true, true)
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

		/// <summary>
		/// If existing photo can be reused.
		/// </summary>
		public bool ReusePhoto { get; } = ReusePhoto;
	}
}
