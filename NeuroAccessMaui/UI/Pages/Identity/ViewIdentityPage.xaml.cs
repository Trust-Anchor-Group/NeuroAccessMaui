namespace NeuroAccessMaui.UI.Pages.Identity
{
	/// <summary>
	/// A page to display when the user wants to view an identity.
	/// </summary>
	public partial class ViewIdentityPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewIdentityPage"/> class.
		/// </summary>
		public ViewIdentityPage(ViewIdentityViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			//!!! this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			//ViewIdentityViewModel ViewModel = this.ViewModel<ViewIdentityViewModel>();

			//!!! Attachment[] Attachments = ViewModel.LegalIdentity?.Attachments;
			//!!! this.PhotoViewer.ShowPhotos(Attachments);
		}
	}
}
