using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	public class ObservablePart : ObservableObject
	{
		public ObservablePart(Part part)
		{
			this.Part = part;
		}
		/// <summary>
		/// Initializes the part, setting properties which needs to be set asynchronosly
		/// </summary>
		/// <param name="contract"></param>
		public async Task InitializeAsync()
		{
			try
			{
				this.FriendlyName = await this.GetFriendlyNameAsync();
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}
		}

		private async Task<string> GetFriendlyNameAsync()
		{
			try
			{
				if (this.Part.LegalId == ServiceRef.TagProfile.TrustProviderId && !string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
					return ServiceRef.TagProfile.Domain;

				ContactInfo info = await Database.FindFirstIgnoreRest<ContactInfo>(new FilterFieldEqualTo("LegalId", this.Part.LegalId));
				if (info is not null && !string.IsNullOrEmpty(info.FriendlyName))
					return info.FriendlyName;
			}
			catch (Exception e)
			{
				//Ignore
			}

			return this.Part.LegalId;
		}
		#region Properties
		/// <summary>
		/// The wrapped Part object
		/// </summary>
		public Part Part { get; }

		public string LegalId => this.Part.LegalId;

		public string Role => this.Part.Role;

		/// <summary>
		/// The friendly name for the part
		/// Has to be initialized with <see cref="InitializeAsync"/>
		/// </summary>
		public string? FriendlyName
		{
			get => this.friendlyName;
			private set => this.friendlyName = value;
		}
		private string? friendlyName = string.Empty;

		#endregion


	}
}
