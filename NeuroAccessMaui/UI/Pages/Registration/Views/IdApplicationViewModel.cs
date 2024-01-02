using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class IdApplicationViewModel : BaseRegistrationViewModel
	{
		public IdApplicationViewModel()
			: base(RegistrationStep.IdApplication)
		{
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			ServiceRef.XmppService.LegalIdentityChanged += this.XmppContracts_LegalIdentityChanged;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppContracts_LegalIdentityChanged;

			await base.OnDispose();
		}

		/// <inheritdoc />
		public override async Task DoAssignProperties()
		{
			await base.DoAssignProperties();

			if (string.IsNullOrEmpty(ServiceRef.TagProfile.Account))
				return;

			if (ServiceRef.TagProfile.LegalIdentity is LegalIdentity LegalIdentity)
			{
				if (LegalIdentity.State == IdentityState.Approved)
					GoToRegistrationStep(RegistrationStep.DefinePin);
				else if (LegalIdentity.Discarded())
				{
					ServiceRef.TagProfile.ClearLegalIdentity();
					GoToRegistrationStep(RegistrationStep.ValidatePhone);
				}
			}
		}

		private async Task XmppContracts_LegalIdentityChanged(object _, LegalIdentityEventArgs e)
		{
			ServiceRef.TagProfile.LegalIdentity = e.Identity;

			await this.DoAssignProperties();
		}

		/// <summary>
		/// If App is connected to the XMPP network.
		/// </summary>
		public static bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;
	}
}
