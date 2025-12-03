using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Contracts;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Resources.Languages;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.UI;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Popups.QR;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	public partial class MyContractsViewModel : BaseViewModel
	{
		private readonly Dictionary<string, ContractReference> contractsMap = [];
		private readonly ContractsListMode contractsListMode;
		private readonly TaskCompletionSource<Contract?>? selection;
		private Contract? selectedContract = null;
		private readonly Dictionary<string, SelectableTag> tagMap = new(StringComparer.OrdinalIgnoreCase);

		private const string AllCategory = "All";
		private const int contractBatchSize = 15;

		private int loadedContracts;

		public ObservableCollection<SelectableTag> FilterTags { get; } = new();

		[ObservableProperty]
		private int hasMore = 0; // 0 means collectionView will load more when scrolles. -1 means event wont be fired.

		[ObservableProperty]
		private bool canShareTemplate;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public MyContractsViewModel(MyContractsNavigationArgs? Args)
		{
			this.IsBusy = true;
			this.Action = SelectContractAction.ViewContract;

			this.loadedContracts = 0;
			this.hasMore = 0;

			if (Args is not null)
			{
				this.contractsListMode = Args.Mode;
				this.Action = Args.Action;
				this.selection = Args.Selection;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						this.Title = ServiceRef.Localizer[nameof(AppResources.Contracts)];
						this.Description = ServiceRef.Localizer[nameof(AppResources.ContractsInfoText)];
						this.CanShareTemplate = false;
						break;

					case ContractsListMode.ContractTemplates:
						this.Title = ServiceRef.Localizer[nameof(AppResources.ContractTemplates)];
						this.Description = ServiceRef.Localizer[nameof(AppResources.ContractTemplatesInfoText)];
						this.CanShareTemplate = true;
						break;

					case ContractsListMode.TokenCreationTemplates:
						this.Title = ServiceRef.Localizer[nameof(AppResources.TokenCreationTemplates)];
						this.Description = ServiceRef.Localizer[nameof(AppResources.TokenCreationTemplatesInfoText)];
						this.CanShareTemplate = true;
						break;
				}
			}
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			this.IsBusy = true;
			this.ShowContractsMissing = false;

			await this.LoadContracts();

			ServiceRef.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			ServiceRef.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.selection is not null && this.selection.Task.IsCompleted)
			{
				await this.GoBack();
				return;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			ServiceRef.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;

			if (this.Action != SelectContractAction.Select)
			{
				this.ShowContractsMissing = false;
				this.contractsMap.Clear();
			}

			this.selection?.TrySetResult(this.selectedContract);

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Gets or sets the title for the view displaying contracts.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Gets or sets the introductory text for the view displaying contracts.
		/// </summary>
		[ObservableProperty]
		private string? description;

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		[ObservableProperty]
		private SelectContractAction action;

		/// <summary>
		/// Gets or sets whether to show a contracts missing alert or not.
		/// </summary>
		[ObservableProperty]
		private bool showContractsMissing;

		/// <summary>
		/// Holds the flat list of contracts to display.
		/// </summary>
		public ObservableCollection<ContractModel> Contracts { get; } = [];

		/// <summary>
		/// Relay command for contract selection from item template
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ContractSelected(object? parameter)
		{
			if (parameter is not ContractModel model)
				return;

			try
			{
				this.IsBusy = true;
				await Task.Yield();
				await this.ContractSelectedAsync(model);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		/// <summary>
		/// Simple in-memory cache of full list for search filtering.
		/// </summary>
		private List<ContractModel> allContracts = [];

		/// <summary>
		/// Search filter text.
		/// </summary>
		private string? searchText;

		/// <summary>
		/// Clears and repopulates the visible Contracts collection based on search.
		/// </summary>
		private void ApplySearchFilter()
		{
			string? s = this.searchText;
			IEnumerable<ContractModel> source = this.allContracts;

			if (!string.IsNullOrWhiteSpace(s))
			{
				string term = s.Trim();
				source = source.Where(c =>
					(c.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(c.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
			}

			foreach (SelectableTag Tag in this.FilterTags)
			{
				string Filter = Tag.GetFilterString();
				if (!string.IsNullOrEmpty(Filter))
				{
					source = source.Where(c => c.Category.Contains(Filter, StringComparison.OrdinalIgnoreCase));
				}
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.Contracts.Clear();
				foreach (ContractModel c in source)
					this.Contracts.Add(c);
			});
		}

		// Check if a contract matches current filter without refiltering the entire list.
		private bool MatchesCurrentFilter(ContractModel c)
		{
			string? s = this.searchText;
			if (!string.IsNullOrWhiteSpace(s))
			{
				string term = s.Trim();
				bool textMatch = (c.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(c.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
				if (!textMatch)
					return false;
			}

			foreach (SelectableTag Tag in this.FilterTags)
			{
				string Filter = Tag.GetFilterString();
				if (!string.IsNullOrEmpty(Filter))
				{
					if (!(c.Category?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false))
						return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Should be called by page on text change.
		/// </summary>
		public void UpdateSearch(string? text)
		{
			this.searchText = text;
			// When search text changes, select the "All" tag and deselect others for clarity and performance.
			MainThread.BeginInvokeOnMainThread(() =>
			{
				SelectableTag? allTag = null;
				foreach (SelectableTag t in this.FilterTags)
				{
					bool isAll = string.Equals(t.Category, AllCategory, StringComparison.OrdinalIgnoreCase);
					if (isAll)
						allTag = t;
					// Deselect all tags first
					t.IsSelected = false;
				}
				if (allTag is not null)
					allTag.IsSelected = true;
			});
			this.ApplySearchFilter();
		}

		/// <summary>
		/// Handle contract selection.
		/// </summary>
		public async Task ContractSelectedAsync(ContractModel Model)
		{
			try
			{
				ContractReference Ref = Model.ContractRef;

				if (Ref.ContractId is null)
				{
					bool Delete = await MainThread.InvokeOnMainThreadAsync(async () =>
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeFound)],
							ServiceRef.Localizer[nameof(AppResources.Yes)],
							ServiceRef.Localizer[nameof(AppResources.No)]));

					if (Delete)
					{
						await Database.FindDelete<ContractReference>(new FilterFieldEqualTo("ContractId", Ref.ContractId)).ConfigureAwait(false);
						await this.LoadContracts();
					}
				}

				switch (this.Action)
				{
					case SelectContractAction.ViewContract:
						if (this.contractsListMode == ContractsListMode.Contracts)
						{
							ViewContractNavigationArgs Args = new(Ref, false);
							await MainThread.InvokeOnMainThreadAsync(async () =>
								await ServiceRef.NavigationService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop));
						}
						else
						{
							await ServiceRef.ContractOrchestratorService.OpenContract(Ref.ContractId, ServiceRef.Localizer[nameof(AppResources.ReferencedID)], null).ConfigureAwait(false);
						}
						break;

					case SelectContractAction.Select:
						Contract? Contract = await Ref.GetContract().ConfigureAwait(false);
						this.selectedContract = Contract;
						await MainThread.InvokeOnMainThreadAsync(async () =>
						{
							await this.GoBack();
						});
						this.selection?.TrySetResult(Contract);
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return;
			}
		}

		// Keep old method signature for any existing callers
		public void ContractSelected(ContractModel Model)
		{
			_ = this.ContractSelectedAsync(Model);
		}

		[RelayCommand]
		private async Task ShareTemplateQR(object? parameter)
		{
			try
			{
				ContractModel? Model = parameter as ContractModel;
				if (Model is null)
					return;

				string ContractUri = Model.ContractIdUriString;
				string ContractName = Model.Category;

				if (string.IsNullOrEmpty(ContractUri))
					return;

				int Width = Constants.QrCode.DefaultImageWidth;
				int Height = Constants.QrCode.DefaultImageHeight;
				byte[] QrBytes = Services.UI.QR.QrCode.GeneratePng(ContractUri, Width, Height);

				ShowQRPopup QrPopup = new ShowQRPopup(QrBytes, ContractUri, ContractName);
				await ServiceRef.PopupService.PushAsync(QrPopup);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task FilterChanged(object? parameter)
		{
			if (parameter is SelectableTag Tag)
			{
				bool wasSelected = Tag.IsSelected;
				if (wasSelected)
				{
					foreach (SelectableTag t in this.FilterTags)
						t.IsSelected = false;
				}
				else
				{
					foreach (SelectableTag t in this.FilterTags)
						t.IsSelected = (t == Tag);
				}
				this.ApplySearchFilter();
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task LoadMoreContracts()
		{
			await this.LoadContracts();
		}

		private async Task LoadContracts()
		{
			try
			{
				IEnumerable<ContractReference> ContractReferences;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", false),
							new FilterFieldEqualTo("ContractLoaded", true)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < contractBatchSize) ? -1 : 0;
						break;

					case ContractsListMode.ContractTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < contractBatchSize) ? -1 : 0;
						break;

					case ContractsListMode.TokenCreationTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < contractBatchSize) ? -1 : 0;
						break;

					default:
						return;
				}

				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory = ServiceRef.NotificationService.GetEventsByCategory(NotificationEventType.Contracts);

				// Dynamically add contracts: append to backing list, and only add to visible list if matching current filter.
				foreach (ContractReference Ref in ContractReferences)
				{
					this.contractsMap[Ref.ContractId] = Ref;

					NotificationEvent[] Events = [];
					if (EventsByCategory.TryGetValue(Ref.ContractId, out NotificationEvent[]? evs))
					{
						EventsByCategory.Remove(Ref.ContractId);
						List<NotificationEvent> Events2 = [];
						foreach (NotificationEvent Event in evs)
						{
							if (Event is not ContractPetitionNotificationEvent)
								Events2.Add(Event);
						}
						Events = [.. Events2];
					}

					ContractModel Item = new(Ref, Events);
					this.allContracts.Add(Item); // append to end

					string? Category = Item.Category;
					if (!string.IsNullOrWhiteSpace(Category))
					{
						MainThread.BeginInvokeOnMainThread(() =>
						{
							if (this.tagMap.TryGetValue(Category, out SelectableTag? Existing))
							{
								Existing.Increment();
							}
							else
							{
								SelectableTag NewTag = new(Category, false, 1);
								this.tagMap[Category] = NewTag;
								this.FilterTags.Add(NewTag);
							}
						});
					}

					// Only add to visible list if it matches current filter
					if (this.MatchesCurrentFilter(Item))
					{
						MainThread.BeginInvokeOnMainThread(() =>
						{
							this.Contracts.Add(Item);
						});
					}
				}

				// Ensure an "All" tag exists showing total count, selected by default
				MainThread.BeginInvokeOnMainThread(() =>
				{
					int total = this.allContracts.Count;
					if (this.tagMap.TryGetValue(AllCategory, out SelectableTag? allTag))
					{
						allTag.IsSelected = true;
						allTag.Count = total;
					}
					else
					{
						SelectableTag all = new(AllCategory, true, total);
						this.tagMap[AllCategory] = all;
						this.FilterTags.Insert(0, all);
					}
				});

				this.ShowContractsMissing = this.allContracts.Count < 1;
			}
			finally
			{
				this.loadedContracts += contractBatchSize;
				this.IsBusy = false;

				
				if (this.HasMore == -1 && this.contractsListMode == ContractsListMode.TokenCreationTemplates)
				{
					foreach (string TokenTemplateId in Constants.ContractTemplates.TokenCreationTemplates)
					{
						ContractReference? Existing = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", TokenTemplateId)).ConfigureAwait(false);

						if (Existing is null)
						{
							Contract? Contract = await ServiceRef.XmppService.GetContract(TokenTemplateId);
							if (Contract is not null)
							{
								ContractReference Ref = new()
								{
									ContractId = Contract.ContractId
								};

								await Ref.SetContract(Contract);
								await Database.Insert(Ref);

								ContractModel Item = new ContractModel(Ref, []);

								this.allContracts.Add(Item);

								// Only add to visible list if it matches current filter
								if (this.MatchesCurrentFilter(Item))
								{
									MainThread.BeginInvokeOnMainThread(() =>
									{
										this.Contracts.Add(Item);
										this.OnPropertyChanged(nameof(this.ShowContractsMissing));
									});
								}
							}
						}
					}
				}
			}
		}

		private Task NotificationService_OnNotificationsDeleted(object? Sender, NotificationEventsArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (NotificationEvent Event in e.Events)
				{
					if (Event.Type != NotificationEventType.Contracts)
						continue;

					foreach (ContractModel Contract in this.Contracts)
					{
						if (Contract.ContractId == Event.Category)
						{
							Contract.RemoveEvent(Event);
							break;
						}
					}
				}
			});

			return Task.CompletedTask;
		}

		private Task NotificationService_OnNewNotification(object? Sender, NotificationEventArgs e)
		{
			if (e.Event.Type == NotificationEventType.Contracts)
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					foreach (ContractModel Contract in this.Contracts)
					{
						if (Contract.ContractId == e.Event.Category)
						{
							Contract.AddEvent(e.Event);
							break;
						}
					}
				});
			}

			return Task.CompletedTask;
		}

		public partial class SelectableTag : ObservableObject
		{
			[ObservableProperty]
			private string category;
			[ObservableProperty]
			private bool isSelected;
			[ObservableProperty]
			private int count;

			public SelectableTag(string Category, bool IsSelected, int Count)
			{
				this.category = Category;
				this.isSelected = IsSelected;
				this.count = Count;
			}

			public string GetFilterString() => this.IsSelected && !string.Equals(this.Category, AllCategory, StringComparison.OrdinalIgnoreCase) ? this.Category : string.Empty;
			public void ToggleSelection() => this.IsSelected = !this.IsSelected;
			public void Increment() => this.Count++;
		}
	}
}
