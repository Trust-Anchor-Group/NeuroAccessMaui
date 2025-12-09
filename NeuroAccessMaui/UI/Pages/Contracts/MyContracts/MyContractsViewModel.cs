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
using Waher.Script;
using NeuroAccessMaui.UI.MVVM;
using CommunityToolkit.Maui.Core.Extensions;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	public partial class MyContractsViewModel : BaseViewModel
	{
		private readonly ContractsListMode contractsListMode;
		private readonly TaskCompletionSource<Contract?>? selection;
		private Contract? selectedContract = null;
		private readonly Dictionary<string, SelectableTag> tagMap = new(StringComparer.OrdinalIgnoreCase);

		public event Action<SelectableTag>? TagSelected;

		private const string AllCategory = "All";
		private const int contractBatchSize = 10;

		private int loadedContracts;
		private string currentCategory;

		public ObservableCollection<SelectableTag> FilterTags { get; set; } = new();

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
			this.currentCategory = AllCategory;

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

			await this.LoadCategories();

			await this.LoadContracts();

			this.ShowContractsMissing = this.Contracts.Count < 1;

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

		[RelayCommand]
		private async Task OpenFilterPopup()
		{
			var vm = new FilterContractsPopupViewModel(this.FilterTags);
			SelectableTag? selectedTag = await ServiceRef.PopupService.PushAsync<FilterContractsPopup, FilterContractsPopupViewModel, MyContractsViewModel.SelectableTag>(vm);
			if (selectedTag is not null)
				await this.FilterChanged(selectedTag);
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
						t.IsSelected = t.Category == AllCategory;

					this.currentCategory = AllCategory;
				}
				else
				{
					foreach (SelectableTag t in this.FilterTags)
						t.IsSelected = (t == Tag);

					this.currentCategory = Tag.Category;
				}

				this.TagSelected?.Invoke(Tag);

				await this.ApplySearchFilter();
			}
		}

		/// <summary>
		/// Should be called by page on text change.
		/// </summary>
		public void UpdateSearch(string? text)
		{
			string? previousCategory = this.currentCategory;

			SelectableTag? selectedTag = null;

			if (string.IsNullOrEmpty(text))
			{
				foreach (SelectableTag Tag in this.FilterTags)
				{
					Tag.IsSelected = false;
				}
				this.FilterTags[0].IsSelected = true; // Select "All"
				selectedTag = this.FilterTags[0];
				this.currentCategory = AllCategory;
			}
			else
			{
				bool found = false; // To ensure only one tag is selected
				foreach (SelectableTag Tag in this.FilterTags)
				{
					if (Tag.Category.Contains(text ?? string.Empty, StringComparison.OrdinalIgnoreCase) && !found)
					{
						found = true;
						Tag.IsSelected = true;
						selectedTag = Tag;
						this.currentCategory = Tag.Category;
					}
					else
					{
						Tag.IsSelected = false;
					}
				}
			}

			if (!string.Equals(previousCategory, this.currentCategory, StringComparison.OrdinalIgnoreCase))
			{
				if (selectedTag is not null)
					this.TagSelected?.Invoke(selectedTag);

				this.ApplySearchFilter().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Clears and repopulates the visible Contracts collection based on search.
		/// </summary>
		private async Task ApplySearchFilter()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				this.Contracts.Clear();
				this.loadedContracts = 0;
				this.HasMore = 0;

			});

			await this.LoadContracts();
		}

		/// <summary>
		/// Determines whether the specified contract matches the currently selected category filter.
		/// </summary>
		/// <param name="c">The contract to evaluate against the current category filter. Cannot be null.</param>
		/// <returns>true if the contract's category matches the current filter or if the filter is set to include all categories;
		/// otherwise, false.</returns>
		private bool MatchesCurrentFilter(ContractModel c) =>
			string.Equals(this.currentCategory, AllCategory, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(this.currentCategory, c.Category, StringComparison.OrdinalIgnoreCase);
		

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
		private async Task LoadMoreContracts()
		{
			await this.LoadContracts();
		}

		private async Task LoadCategories()
		{
			try
			{
				object Categories = await Expression.EvalAsync($"select distinct Category from NeuroAccessMaui.Services.Contracts.ContractReference where IsTemplate={this.contractsListMode != ContractsListMode.Contracts}");

				if (Categories is not object[] Items)
					return;

				foreach (object Item in Items)
				{
					if (Item is string category && !string.IsNullOrWhiteSpace(category))
					{
						MainThread.BeginInvokeOnMainThread(() =>
						{
							if (!this.tagMap.ContainsKey(category))
							{
								SelectableTag NewTag = new(category, false);
								this.tagMap[category] = NewTag;
								this.FilterTags.Add(NewTag);
							}
						});
					}
				}

				// Ensure an "All" tag exists showing total count, selected by default
				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (this.tagMap.TryGetValue(AllCategory, out SelectableTag? allTag))
					{
						allTag.IsSelected = true;
					}
					else
					{
						SelectableTag all = new(AllCategory, true);
						this.tagMap[AllCategory] = all;
						this.FilterTags.Insert(0, all);
					}
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task LoadContracts()
		{
			try
			{
				IEnumerable<ContractReference>? ContractReferences = await this.LoadFromDatabase();

				if (ContractReferences is null)
				{
					this.HasMore = -1;
					return;
				}

				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory = ServiceRef.NotificationService.GetEventsByCategory(NotificationEventType.Contracts);

				foreach (ContractReference Ref in ContractReferences)
				{
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

					// Only add to visible list if it matches current filter
					if (this.MatchesCurrentFilter(Item))
					{
						MainThread.BeginInvokeOnMainThread(() =>
						{
							this.Contracts.Add(Item);
						});
					}
				}
			}
			finally
			{
				this.loadedContracts += contractBatchSize;
				this.IsBusy = false;

				if (this.HasMore == -1 && this.contractsListMode == ContractsListMode.TokenCreationTemplates)
				{
					foreach (string TokenTemplateId in Constants.ContractTemplates.TokenCreationTemplates)
					{
						ContractReference? Existing = await Database.FindFirstDeleteRest<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("ContractId", TokenTemplateId)
						));

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

				/*
				foreach (ContractModel Model in this.allContracts)
				{
					ContractReference Ref = Model.ContractRef;

					Ref.ObjectId = null;

					await Database.Insert(Ref);
				}
				*/
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

		private async Task<IEnumerable<ContractReference>?> LoadFromDatabase()
		{
			IEnumerable<ContractReference> ContractReferences;

			if (this.currentCategory == AllCategory)
			{
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
						return null;
				}
			}
			else
			{
				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", false),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("Category", this.currentCategory)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < contractBatchSize) ? -1 : 0;
						break;

					case ContractsListMode.ContractTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("Category", this.currentCategory)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < contractBatchSize) ? -1 : 0;
						break;

					case ContractsListMode.TokenCreationTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("Category", this.currentCategory)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
					this.HasMore = (ContractReferences.Count() < contractBatchSize) ? -1 : 0;
						break;

					default:
						return null;
				}
			}

			return ContractReferences;
		}

		public partial class SelectableTag : ObservableObject
		{
			[ObservableProperty]
			private string category;
			[ObservableProperty]
			private bool isSelected;

			public SelectableTag(string Category, bool IsSelected)
			{
				this.category = Category;
				this.isSelected = IsSelected;
			}

			public string GetFilterString() => this.IsSelected && !string.Equals(this.Category, AllCategory, StringComparison.OrdinalIgnoreCase) ? this.Category : string.Empty;
			public void ToggleSelection() => this.IsSelected = !this.IsSelected;
		}
	}
}
