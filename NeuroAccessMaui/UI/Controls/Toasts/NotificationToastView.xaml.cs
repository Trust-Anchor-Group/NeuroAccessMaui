using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls.Toasts
{
	/// <summary>
	/// A toast that shows the title and body of a notification. Tapping it runs an action once.
	/// </summary>
	public partial class NotificationToastView : ContentView
	{
		private readonly Func<Task>? tapped;
		private bool handled;

		/// <summary>
		/// A toast that shows the title and body of a notification. Tapping it runs an action once.
		/// </summary>
		/// <param name="Title">The title.</param>
		/// <param name="Body">The body, if any. Plain text.</param>
		/// <param name="Tapped">What to do when the toast is tapped, if anything.</param>
		public NotificationToastView(string Title, string? Body, Func<Task>? Tapped)
		{
			this.InitializeComponent();

			this.tapped = Tapped;
			this.TitleLabel.Text = Title;
			this.BodyLabel.Text = Body;
			this.BodyLabel.IsVisible = !string.IsNullOrEmpty(Body);

			SemanticProperties.SetDescription(this, string.IsNullOrEmpty(Body) ? Title : Title + ". " + Body);
		}

		private async void OnTapped(object? Sender, TappedEventArgs e)
		{
			if (this.handled || this.tapped is null)
				return;

			this.handled = true;

			try
			{
				await this.tapped();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
