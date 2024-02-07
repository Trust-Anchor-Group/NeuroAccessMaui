using System.ComponentModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionContract
{
	/// <summary>
	/// A page to display when the user is asked to petition a contract.
	/// </summary>
	[DesignTimeVisible(true)]
	public partial class PetitionContractPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PetitionContractPage"/> class.
		/// </summary>
		public PetitionContractPage()
		{
			this.ContentPageModel = new PetitionContractViewModel();
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			if (this.ContentPageModel is PetitionContractViewModel PetitionContractViewModel)
			{
				Attachment[]? Attachments = PetitionContractViewModel.RequestorIdentity?.Attachments;
				if (Attachments is null)
					this.PhotoViewer.HidePhotos();
				else
					this.PhotoViewer.ShowPhotos(Attachments);
			}
		}
	}
}
