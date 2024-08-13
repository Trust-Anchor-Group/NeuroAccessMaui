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
		private ObservableContract(Contract contract)
		{
			this.Contract = contract;
			this.RegisterEventHandlers();
		}

	    ~ObservableContract()
		{
			this.UnregisterEventHandlers();
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="ObservableContract"/> and initializes the roles and parameters.
		/// </summary>
		/// <param name="contract">The Contract object to wrap</param>
		public static async Task<ObservableContract> CreateAsync(Contract contract)
		{
			ObservableContract Contract = new(contract);
			await Contract.InitializeAsync();
			return Contract;
		}

		private async Task InitializeAsync()
		{
			this.Category = await ContractModel.GetCategory(this.Contract) ?? string.Empty;

			foreach (Parameter P in this.Contract.Parameters)
			{
				this.Parameters.Add(await ObservableParameter.CreateAsync(P, this.Contract));
			}

		}

		private void RegisterEventHandlers()
		{
			foreach (ObservableParameter P in this.Parameters)
			{
				P.PropertyChanged += this.Parameter_OnPropertyChanged;
			}

			foreach (ObservableRole R in this.Roles)
			{
				R.PropertyChanged += this.Role_OnPropertyChanged;
			}
			this.Parameters.CollectionChanged += this.Parameters_CollectionChanged;
			this.Roles.CollectionChanged += this.Roles_CollectionChanged;
		}

		private void UnregisterEventHandlers()
		{
			foreach (ObservableParameter P in this.Parameters)
			{
				P.PropertyChanged += this.Parameter_OnPropertyChanged;
			}

			foreach (ObservableRole R in this.Roles)
			{
				R.PropertyChanged += this.Role_OnPropertyChanged;
			}

			this.Parameters.CollectionChanged -= this.Parameters_CollectionChanged;
			this.Roles.CollectionChanged -= this.Roles_CollectionChanged;
		}

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

		private void Parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{

			if(e.Action == NotifyCollectionChangedAction.Add)
			{
				if(e.NewItems is null)
					return;
				foreach (ObservableParameter P in e.NewItems)
				{
					Console.WriteLine("Parameter_CollectionChanged: " + P.Parameter.Name);
					P.PropertyChanged += this.Parameter_OnPropertyChanged;
				}
			}
			else if(e.Action == NotifyCollectionChangedAction.Remove)
			{
				if(e.OldItems is null)
					return;
				foreach (ObservableParameter P in e.OldItems)
				{
					P.PropertyChanged -= this.Parameter_OnPropertyChanged;
				}
			}
		}

		private void Roles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if(e.Action == NotifyCollectionChangedAction.Add)
			{
				if(e.NewItems is null)
					return;
				foreach (ObservableRole R in e.NewItems)
				{
					R.PropertyChanged += this.Role_OnPropertyChanged;
				}
			}
			else if(e.Action == NotifyCollectionChangedAction.Remove)
			{
				if(e.OldItems is null)
					return;
				foreach (ObservableRole R in e.OldItems)
				{
					R.PropertyChanged -= this.Role_OnPropertyChanged;
				}
			}
		}

		protected override void OnPropertyChanging(System.ComponentModel.PropertyChangingEventArgs e)
		{
			if(e.PropertyName == nameof(this.Parameters) || e.PropertyName == nameof(this.Roles))
			{
				this.UnregisterEventHandlers();
			}
		}
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if(e.PropertyName == nameof(this.Parameters) || e.PropertyName == nameof(this.Roles))
			{
				this.RegisterEventHandlers();
			}
			base.OnPropertyChanged(e);
		}

		

		/// <summary>
		/// The wrapped contract object
		/// </summary>
		public Contract Contract { get; }

		/// <summary>
		/// The Parameters of the contract
		/// </summary>
		private ObservableCollection<ObservableParameter> parameters = [];
		public ObservableCollection<ObservableParameter> Parameters
		{
			get => this.parameters;
			private set => this.SetProperty(ref this.parameters, value);
		}

		/// <summary>
		/// The Roles of the contract
		/// </summary>
		private ObservableCollection<ObservableRole> roles = [];
		public ObservableCollection<ObservableRole> Roles
		{
			get => this.roles;
			private set => this.SetProperty(ref this.roles, value);
		}

		public ContractState ContractState => this.ContractState; 

		/// <summary>
		/// if the contract can act as a template
		/// </summary>
		public bool CanActAsTemplate => this.Contract.CanActAsTemplate;
		/// <summary>
		/// if the contract is a template
		/// </summary>
		public bool IsTemplate => this.Contract.PartsMode == ContractParts.TemplateOnly;

		/// <summary>
		/// The Category of the contract
		/// </summary>
		public string Category
		{
			get => this.category;
			private set => this.SetProperty(ref this.category, value);
		}
		private string category = "";


	}
}
