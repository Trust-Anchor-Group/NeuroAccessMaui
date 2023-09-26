namespace NeuroAccessMaui.Pages.Petitions;

/// <summary>
/// A page to display when the user is asked to petition an identity.
/// </summary>
public partial class PetitionIdentityPage
{
	/// <summary>
	/// Creates a new instance of the <see cref="PetitionIdentityPage"/> class.
	/// </summary>
	public PetitionIdentityPage(PetitionIdentityViewModel ViewModel)
	{
		this.InitializeComponent();
		this.BindingContext = ViewModel;
	}

	/// <inheritdoc/>
	protected override Task OnDisappearingAsync()
	{
		//!!! this.PhotoViewer.HidePhotos();
		return base.OnDisappearingAsync();
	}

	private void Image_Tapped(object Sender, EventArgs e)
	{
		PetitionIdentityViewModel ViewModel = this.ViewModel<PetitionIdentityViewModel>();

		//!!! Attachment[] Attachments = ViewModel.RequestorIdentity?.Attachments;
		//!!! this.PhotoViewer.ShowPhotos(Attachments);
	}
}
