using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Contract"/> object.
	/// This allows for easier binding in the UI.
	/// Either create instances with <see cref="CreateAsync"/> or initialize with <see cref="InitializeAsync"/>.
	/// </summary>
	public class ObservableContract : ObservableObject, IDisposable
	{

		#region Constructors and Destructor

		public ObservableContract(Contract contract)
		{
			this.Contract = contract;
			this.Parameters.CollectionChanged += this.Parameters_CollectionChanged;
			this.Roles.CollectionChanged += this.Roles_CollectionChanged;
		}

		#endregion

		#region Initialization

		/// <summary>
		/// Creates a new instance of <see cref="ObservableContract"/> and initializes the roles and parameters.
		/// </summary>
		/// <param name="contract">The Contract object to wrap.</param>
		public static async Task<ObservableContract> CreateAsync(Contract contract)
		{
			ObservableContract contractWrapper = new(contract);
			await contractWrapper.InitializeAsync();
			return contractWrapper;
		}

		/// <summary>
		/// Initializes the contract data, such as category and parameters.
		/// </summary>
		public async Task InitializeAsync()
		{
			this.Category = await ContractModel.GetCategory(this.Contract) ?? string.Empty;

			foreach (Parameter param in this.Contract.Parameters ?? Enumerable.Empty<Parameter>())
			{
				ObservableParameter observableParam = await ObservableParameter.CreateAsync(param, this.Contract);
				this.Parameters.Add(observableParam);
			}

			foreach (Role role in this.Contract.Roles ?? Enumerable.Empty<Role>())
			{
				ObservableRole observableRole = new(role);
				await observableRole.InitializeAsync(this.Contract);
				this.Roles.Add(observableRole);
			}
			if (this.IsTemplate)
			{
				foreach (Part part in this.Contract.Parts ?? Enumerable.Empty<Part>())
				{
					ObservableRole? Role = this.Roles.FirstOrDefault(r => r.Name == part.Role);
					if (Role is not null)
						await Role.AddPart(part);
				}
			}
			else
			{
				foreach (ClientSignature signature in this.Contract.ClientSignatures ?? Enumerable.Empty<ClientSignature>())
				{
					ObservableRole? Role = this.Roles.FirstOrDefault(r => r.Name == signature.Role);
					if (Role is not null)
						await Role.AddPart(signature);
				}
			}

		}

		#endregion

		#region Collection Changed Handlers

		private void Parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
			{
				foreach (ObservableParameter param in e.NewItems)
				{
					param.PropertyChanged += this.Parameter_OnPropertyChanged;
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
			{
				foreach (ObservableParameter param in e.OldItems)
				{
					param.PropertyChanged -= this.Parameter_OnPropertyChanged;
				}
			}
		}

		private void Roles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
			{
				foreach (ObservableRole role in e.NewItems)
				{
					role.PropertyChanged += this.Role_OnPropertyChanged;
					role.Parts.CollectionChanged += this.Parts_CollectionChanged;
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
			{
				foreach (ObservableRole role in e.OldItems)
				{
					role.PropertyChanged -= this.Role_OnPropertyChanged;
					role.Parts.CollectionChanged -= this.Parts_CollectionChanged;
				}
			}
		}

		#endregion

		#region Property Changed Handlers

		public event NotifyCollectionChangedEventHandler? PartChanged;
		private void Parts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			this.PartChanged?.Invoke(sender, e);
		}

		/// <summary>
		/// Occurs when a property value of any parameter changes.
		/// </summary>
		public event PropertyChangedEventHandler? ParameterChanged;

		private void Parameter_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			this.ParameterChanged?.Invoke(sender, e);
		}

		/// <summary>
		/// Occurs when a property value of any role changes.
		/// </summary>
		public event PropertyChangedEventHandler? RoleChanged;

		private void Role_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			this.RoleChanged?.Invoke(sender, e);
		}

		#endregion

		#region Properties

		/// <summary>
		/// The wrapped contract object.
		/// </summary>
		public Contract Contract { get; }

		/// <summary>
		/// The parameters of the contract.
		/// </summary>
		public ObservableCollection<ObservableParameter> Parameters => this.parameters;
		private readonly ObservableCollection<ObservableParameter> parameters = [];

		/// <summary>
		/// The roles of the contract.
		/// </summary>
		public ObservableCollection<ObservableRole> Roles => this.roles;
		private readonly ObservableCollection<ObservableRole> roles = [];

		/// <summary>
		/// The category of the contract.
		/// </summary>
		public string Category
		{
			get => this.category;
			private set => this.SetProperty(ref this.category, value);
		}
		private string category = string.Empty;

		/// <summary>
		/// The contract state.
		/// </summary>
		public ContractState ContractState => this.Contract.State;

		/// <summary>
		/// Whether the contract can act as a template.
		/// </summary>
		public bool CanActAsTemplate => this.Contract.CanActAsTemplate;

		/// <summary>
		/// Whether the contract is a template.
		/// </summary>
		public bool IsTemplate => this.Contract.PartsMode == ContractParts.TemplateOnly;

		/// <summary>
		/// The visibility of the contract.
		/// </summary>
		public ContractVisibility Visibility => this.Contract.Visibility;

		/// <summary>
		/// The contract ID.
		/// </summary>
		public string ContractId => this.Contract.ContractId;

		/// <summary>
		/// The template ID.
		/// </summary>
		public string TemplateId => this.Contract.TemplateId;

		#endregion
				private bool disposed = false;

		#region IDisposable Support
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				// Unsubscribe from the event to prevent memory leaks
				this.Parameters.CollectionChanged -= this.Parameters_CollectionChanged;
				this.Roles.CollectionChanged -= this.Roles_CollectionChanged;
			}

			this.disposed = true;
		}
		#endregion
	}
}
