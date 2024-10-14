using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Contract"/> object.
	/// This allows for easier binding in the UI.
	/// Either create instances with <see cref="CreateAsync"/> or initiailize with <see cref="InitializeAsync"/>
	/// </summary>
	public class ObservableContract : ObservableObject
	{
		#region Constructors and Destructor
		private ObservableContract(Contract contract)
		{
			this.Contract = contract;
			this.RegisterEventHandlers();
		}

	    ~ObservableContract()
		{
			this.UnregisterEventHandlers();
		}
		#endregion

		#region Initialization
		/// <summary>
		/// Creates a new instance of <see cref="ObservableContract"/> and initializes the roles and parameters.
		/// </summary>
		/// <param name="contract">The Contract object to wrap</param>
		public static async Task<ObservableContract> CreateAsync(Contract contract)
		{
			ObservableContract contractWrapper = new(contract);
			await contractWrapper.InitializeAsync();
			return contractWrapper;
		}

		/// <summary>
		/// Initializes the contract data, such as category and parameters.
		/// </summary>
		private async Task InitializeAsync()
		{
			this.Category = await ContractModel.GetCategory(this.Contract) ?? string.Empty;

			foreach (Parameter param in this.Contract.Parameters)
			{
				this.Parameters.Add(await ObservableParameter.CreateAsync(param, this.Contract));
			}
		}
		#endregion

		#region Event Handlers Registration/Unregistration
		private void RegisterEventHandlers()
		{
			foreach (ObservableParameter param in this.Parameters)
			{
				param.PropertyChanged += this.Parameter_OnPropertyChanged;
			}

			foreach (ObservableRole role in this.Roles)
			{
				role.PropertyChanged += this.Role_OnPropertyChanged;
			}

			this.Parameters.CollectionChanged += this.Parameters_CollectionChanged;
			this.Roles.CollectionChanged += this.Roles_CollectionChanged;
		}

		private void UnregisterEventHandlers()
		{
			foreach (ObservableParameter param in this.Parameters)
			{
				param.PropertyChanged -= this.Parameter_OnPropertyChanged;
			}

			foreach (ObservableRole role in this.Roles)
			{
				role.PropertyChanged -= this.Role_OnPropertyChanged;
			}

			this.Parameters.CollectionChanged -= this.Parameters_CollectionChanged;
			this.Roles.CollectionChanged -= this.Roles_CollectionChanged;
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
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
			{
				foreach (ObservableRole role in e.OldItems)
				{
					role.PropertyChanged -= this.Role_OnPropertyChanged;
				}
			}
		}
		#endregion

		#region Property Change Handlers
		public event PropertyChangedEventHandler? ParameterChanged;
		private void Parameter_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			this.ParameterChanged?.Invoke(sender, e);
		}

		public event PropertyChangedEventHandler? RoleChanged;
		private void Role_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			this.RoleChanged?.Invoke(sender, e);
		}

		protected override void OnPropertyChanging(System.ComponentModel.PropertyChangingEventArgs e)
		{
			if (e.PropertyName == nameof(this.Parameters) || e.PropertyName == nameof(this.Roles))
			{
				this.UnregisterEventHandlers();
			}
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.Parameters) || e.PropertyName == nameof(this.Roles))
			{
				this.RegisterEventHandlers();
			}
			base.OnPropertyChanged(e);
		}
		#endregion

		#region Properties
		/// <summary>
		/// The wrapped contract object
		/// </summary>
		public Contract Contract { get; }

		/// <summary>
		/// The Parameters of the contract
		/// </summary>
		private ObservableCollection<ObservableParameter> parameters = new();
		public ObservableCollection<ObservableParameter> Parameters
		{
			get => this.parameters;
			private set => this.SetProperty(ref this.parameters, value);
		}

		/// <summary>
		/// The Roles of the contract
		/// </summary>
		private ObservableCollection<ObservableRole> roles = new();
		public ObservableCollection<ObservableRole> Roles
		{
			get => this.roles;
			private set => this.SetProperty(ref this.roles, value);
		}

		/// <summary>
		/// The Category of the contract
		/// </summary>
		private string category = string.Empty;
		public string Category
		{
			get => this.category;
			private set => this.SetProperty(ref this.category, value);
		}

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
		#endregion
	}
}
