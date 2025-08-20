using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Role"/> object.
	/// This allows for easier binding in the UI.
	/// </summary>
	public partial class ObservableRole : ObservableObject
	{
		#region Constructor
		public ObservableRole(Role role)
		{
			this.Role = role;
			this.parts = new ObservableCollection<ObservablePart>();
		}


		#endregion

		#region Initialization
		/// <summary>
		/// Initializes the role in regards to a contract.
		/// E.g Sets the description of the role, with the contract language.
		/// </summary>
		/// <param name="contract"></param>
		public async Task InitializeAsync(Contract contract)
		{
			try
			{
				// Set the description based on the contract language
				string UntrimmedDescription = await contract.ToPlainText(this.Role.Descriptions, contract.DeviceLanguage());
				this.Description = UntrimmedDescription.Trim();

				// Add the parts associated with the role
				if (contract.Parts is null)
					return;
				foreach (Part Part in contract.Parts)
				{
					if (Part.Role == this.Name)
					{
						await this.AddPart(Part);
					}
				}
			}
			catch (Exception E)
			{
				ServiceRef.LogService.LogException(E);
			}
		}
		#endregion


		#region Properties
		/// <summary>
		/// The wrapped Role object
		/// </summary>
		public Role Role { get; }

		/// <summary>
		/// The parts associated with the role
		/// </summary>
		public ObservableCollection<ObservablePart> Parts
		{
			get => this.parts;
			private set => this.SetProperty(ref this.parts, value);
		}
		private ObservableCollection<ObservablePart> parts;

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
				if (this.SetProperty(ref this.description, value))
				{
					this.OnPropertyChanged(nameof(this.Label));
				}
			}
		}
		private string description = string.Empty;

		/// <summary>
		/// Label to display in the UI, defaults to Name if Description is not set.
		/// </summary>
		public string Label => string.IsNullOrEmpty(this.Description) ? this.Name : this.Description;

		/// <summary>
		/// Largest amount of signatures of this role required for a legally binding contract.
		/// </summary>
		public int MaxCount => this.Role.MaxCount;

		/// <summary>
		/// Smallest amount of signatures of this role required for a legally binding contract.
		/// </summary>
		public int MinCount => this.Role.MinCount;

		/// <summary>
		/// If the role has reached the maximum amount of parts.
		/// </summary>
		public bool HasReachedMaxCount => this.Parts.Count >= this.MaxCount;

		/// <summary>
		/// If the role has reached the minimum amount of parts.
		/// </summary>
		public bool HasReachedMinCount => this.Parts.Count >= this.MinCount;

		/// <summary>
		/// True if user can select themselves for this role (not at max or already selected).
		/// </summary>
		public bool CanSelectMe
		{
			get
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				bool AlreadySelected = !string.IsNullOrEmpty(MyId) && this.Parts.Any(p => p.LegalId == MyId);
				return AlreadySelected || !this.HasReachedMaxCount;
			}
		}

		/// <summary>
		/// If current user has selected to sign as this role. Toggling this will add/remove "Me" as a part,
		/// respecting MaxCount. Does nothing if no current LegalId is available.
		/// </summary>
		public bool IsSelectedByMe
		{
			get
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (string.IsNullOrEmpty(MyId))
					return false;
				return this.Parts.Any(p => p.LegalId == MyId);
			}
			set
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (string.IsNullOrEmpty(MyId))
				{
					// No identity selected; ignore.
					return;
				}

				bool CurrentlySelected = this.Parts.Any(p => p.LegalId == MyId);
				if (value == CurrentlySelected)
					return;

				if (value)
				{
					if (this.Parts.Count >= this.MaxCount)
					{
						// Cannot add, would exceed max. Simply ignore.
						return;
					}
					_ = this.AddPart(MyId, false);
				}
				else
				{
					this.RemovePart(MyId, false);
				}

				this.OnPropertyChanged();
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Adds a part with a given LegalId to the role.
		/// </summary>
		/// <param name="LegalId"></param>
		public async Task AddPart(string LegalId, bool Notify = true, bool AutoPetition = true, bool PresetFromArgs = false)
		{
			//Check if Part Exists
			if (this.Parts.Any(p => string.Equals(p.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)))
				return;

			//Create Part
			Part Part = new() { LegalId = LegalId, Role = this.Name };
			ObservablePart ObservablePart = new ObservablePart(Part)
			{
				IsPresetFromArgs = PresetFromArgs
			};
			await ObservablePart.InitializeAsync(AutoPetition);

			//Notify changes
			TaskCompletionSource TaskCompletionSource = new();
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.Parts.Add(ObservablePart);
				this.OnPropertyChanged(nameof(this.HasReachedMaxCount));
				this.OnPropertyChanged(nameof(this.HasReachedMinCount));
				this.OnPropertyChanged(nameof(this.IsSelectedByMe));
				if (Notify)
					this.OnPropertyChanged(nameof(this.Parts));
				TaskCompletionSource.SetResult();
			});
			await TaskCompletionSource.Task;
		}

		/// <summary>
		/// Adds a part object to the role.
		/// </summary>
		public async Task AddPart(Part part)
		{
			await this.AddPart(part.LegalId);
		}

		/// <summary>
		/// Add a part from a signature, or mark the part as signed if it already exists.
		/// </summary>
		public async Task AddPart(ClientSignature signature)
		{
			await this.AddPart(signature.LegalId, true, false);
			ObservablePart? Found = this.Parts.FirstOrDefault(p => p.LegalId == signature.LegalId);
			if (Found is not null)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					Found.Signature = signature;
				});
				await Found.InitializeAsync(false);
			}
		}

		/// <summary>
		/// Removes a part with a given LegalId from the role.
		/// </summary>
		public void RemovePart(string LegalId, bool Notify = true)
		{
			ObservablePart? Part = this.Parts.FirstOrDefault(p => p.LegalId == LegalId);
			if (Part is not null)
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.Parts.Remove(Part);
					this.OnPropertyChanged(nameof(this.HasReachedMaxCount));
					this.OnPropertyChanged(nameof(this.HasReachedMinCount));
					this.OnPropertyChanged(nameof(this.IsSelectedByMe));
					if (Notify)
						this.OnPropertyChanged(nameof(this.Parts));
				});
			}
		}

		/// <summary>
		/// Removes a part object from the role.
		/// </summary>
		public void RemovePart(Part part)
		{
			this.RemovePart(part.LegalId);
		}

		[RelayCommand]
		private void RemovePart(ObservablePart? part)
		{
			if (part is null)
				return;
			this.RemovePart(part.LegalId);

		}
		#endregion

		#region Commands
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task AddPartFromContacts()
		{
			IEnumerable<ContactInfo> Contacts = await Database.Find<ContactInfo>();
			TaskCompletionSource<ContactInfoModel?> Selected = new();
			ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.AddContactToContract)], Selected)
			{
				CanScanQrCode = true,
				Contacts = Contacts
			};

			await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);

			ContactInfoModel? Contact = await Selected.Task;
			if (Contact is null)
				return;

			string LegalId = Contact.LegalId;

			await this.AddPart(LegalId);
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task AddPartFromQr()
		{
			string? Code = await QrCode.ScanQrCode(ServiceRef.Localizer[nameof(AppResources.ScanQRCode)], [Constants.UriSchemes.IotId]);
			if (string.IsNullOrEmpty(Code))
				return;

			string LegalId = Constants.UriSchemes.RemoveScheme(Code) ?? string.Empty;

			await this.AddPart(LegalId);
		}
		#endregion
	}
}
