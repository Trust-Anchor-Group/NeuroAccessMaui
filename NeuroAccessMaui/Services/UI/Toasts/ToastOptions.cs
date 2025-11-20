using System;

namespace NeuroAccessMaui.Services.UI.Toasts
{
	/// <summary>
	/// Options controlling toast presentation.
	/// </summary>
	public class ToastOptions
	{
		/// <summary>
		/// Gets or sets the transition used when the toast is shown.
		/// </summary>
		public ToastTransition ShowTransition { get; set; } = ToastTransition.SlideFromTop;

		/// <summary>
		/// Gets or sets the transition used when the toast is dismissed.
		/// </summary>
		public ToastTransition HideTransition { get; set; } = ToastTransition.SlideFromTop;

		/// <summary>
		/// Gets or sets whether the toast is dismissed automatically after <see cref="Duration"/>.
		/// </summary>
		public bool AutoDismiss { get; set; } = true;

		/// <summary>
		/// Gets or sets how long the toast remains visible when <see cref="AutoDismiss"/> is enabled.
		/// </summary>
		public TimeSpan Duration
		{
			get => this.duration;
			set
			{
				if (value < TimeSpan.Zero)
					this.duration = TimeSpan.Zero;
				else
					this.duration = value;
			}
		}

		/// <summary>
		/// Gets or sets the preferred placement for the toast overlay.
		/// </summary>
		public ToastPlacement Placement { get; set; } = ToastPlacement.Top;

		private TimeSpan duration = TimeSpan.FromSeconds(3);
	}
}
