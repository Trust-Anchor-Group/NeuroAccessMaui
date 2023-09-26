using System;
using System.ComponentModel;

namespace IdApp.Pages.Identity.TransferIdentity
{
    /// <summary>
    /// A page to display when the user wants to view an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class TransferIdentityPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TransferIdentityPage"/> class.
        /// </summary>
        public TransferIdentityPage()
        {
            this.ViewModel = new TransferIdentityViewModel();

			this.InitializeComponent();
        }
    }
}
