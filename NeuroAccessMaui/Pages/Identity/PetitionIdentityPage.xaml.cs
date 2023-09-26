using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Identity.PetitionIdentity
{
    /// <summary>
    /// A page to display when the user is asked to petition an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class PetitionIdentityPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PetitionIdentityPage"/> class.
        /// </summary>
        public PetitionIdentityPage()
        {
            this.ViewModel = new PetitionIdentityViewModel();
			this.InitializeComponent();
        }

        /// <inheritdoc/>
        protected override Task OnDisappearingAsync()
        {
            this.PhotoViewer.HidePhotos();
            return base.OnDisappearingAsync();
        }

        private void Image_Tapped(object Sender, EventArgs e)
        {
			if (this.ViewModel is PetitionIdentityViewModel PetitionIdentityViewModel)
			{
				Attachment[] attachments = PetitionIdentityViewModel.RequestorIdentity?.Attachments;
				this.PhotoViewer.ShowPhotos(attachments);
			}
        }
    }
}
