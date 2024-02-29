using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// Represents an icon for an URI schema.
	/// </summary>
	/// <param name="Geometry">Geometry of icon.</param>
	/// <param name="ForegroundColor">Color of icon.</param>
	/// <param name="ParentViewModel">View model of QR-code scanning view.</param>
	public class UriSchemaIcon(Geometry Geometry, Color ForegroundColor, ScanQrCodeViewModel ParentViewModel) : BindableObject()
	{
		private readonly ScanQrCodeViewModel parentViewModel = ParentViewModel;

		/// <summary>
		/// Geometry of icon
		/// </summary>
		public Geometry Geometry { get; } = Geometry;

		/// <summary>
		/// Foreground Color of icon
		/// </summary>
		public Color ForegroundColor { get; } = ForegroundColor;

		/// <summary>
		/// Background color of displayed icon.
		/// </summary>
		public Color BackgroundColor => this.parentViewModel.IconBackgroundColor;

		/// <summary>
		/// Called when background color has changed.
		/// </summary>
		internal void BackgroundColorChanged()
		{
			this.OnPropertyChanged(nameof(this.BackgroundColor));
		}
	}
}
