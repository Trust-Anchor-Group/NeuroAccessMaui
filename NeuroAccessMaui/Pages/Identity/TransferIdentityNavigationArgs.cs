using IdApp.Services.Navigation;

namespace IdApp.Pages.Identity.TransferIdentity
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying an identity transfer code.
    /// </summary>
    public class TransferIdentityNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TransferIdentityNavigationArgs"/> class.
        /// </summary>
        public TransferIdentityNavigationArgs() { }

        /// <summary>
        /// Creates a new instance of the <see cref="TransferIdentityNavigationArgs"/> class.
        /// </summary>
        /// <param name="Uri">Transfer URI</param>
        public TransferIdentityNavigationArgs(string Uri)
        {
            this.Uri = Uri;
        }

        /// <summary>
        /// Transfer URI
        /// </summary>
        public string Uri { get; }
    }
}