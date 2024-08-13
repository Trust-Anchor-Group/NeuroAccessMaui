using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Role"/> object.
	/// This allows for easier binding in the UI.
	/// </summary>
	public class ObservableRole : ObservableObject
	{
		public ObservableRole(Role role)
		{
			this.Role = role;
		}

		/// <summary>
		/// Initializes the role in regards to a contract.
		/// E.g Sets the description of the role, with the contract language.
		/// </summary>
		/// <param name="contract"></param>
		public async Task InitializeAsync(Contract contract)
		{
			try 
			{
				this.Description = await contract.ToPlainText(this.Role.Descriptions, contract.DeviceLanguage());
				foreach (Part P in contract.Parts)
				{
					if(P.Role == this.Name)
						this.AddPart(P);
				}			
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}

		}

		/// <summary>
		/// The wrapped Role object
		/// </summary>
		public Role Role { get; }

		/// <summary>
		/// The parts associated with the role
		/// </summary>
		public ObservableCollection<Part> Parts { get => this.parts; private set => this.SetProperty(ref this.parts, value); }
		private ObservableCollection<Part> parts = [];

		/// <summary>
		/// The name of the role
		/// </summary>
		public string Name => this.Role.Name;

		/// <summary>
		/// The localized description of the role
		/// Has to be initialized with <see cref="InitializeAsync"/>
		/// </summary>
		public string Description
		{
			get => this.description;
			private set
			{
				this.SetProperty(ref this.description, value);
				this.OnPropertyChanged(nameof(this.Label));
			}
		}
		private string description = string.Empty;

		public string Label => string.IsNullOrEmpty(this.Description) ? this.Name : this.Description;

		/// <summary>
		/// Largest amount of signatures of this role required for a legally binding contract.    
		/// /// </summary>
		public int MaxCount => this.Role.MaxCount;

		/// <summary>
		/// Smallest amount of signatures of this role required for a legally binding contract.    
		/// /// </summary>
		public int MinCount => this.Role.MinCount;


		public void AddPart(string LegalId)
		{
			this.Parts.Add(new Part{LegalId = LegalId, Role = this.Name});
		}
		public void AddPart(Part part)
		{
			this.Parts.Add(part);
		}
	}
}
