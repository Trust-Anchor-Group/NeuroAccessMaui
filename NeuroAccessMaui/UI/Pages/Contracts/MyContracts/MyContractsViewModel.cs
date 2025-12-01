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
	/// <summary>
	/// The view model to bind to when displaying 'my' contracts.
	/// TODO: This page and ViewModel should be refactored
	/// </summary>
	public partial class MyContractsViewModel : BaseViewModel
	{
		private readonly Dictionary<string, ContractReference> contractsMap = [];
		private readonly ContractsListMode contractsListMode;
		private readonly TaskCompletionSource<Contract?>? selection;
		private Contract? selectedContract = null;
		private readonly Dictionary<string, SelectableTag> tagMap = new(StringComparer.OrdinalIgnoreCase); // Map for quick tag lookup

		private const string AllCategory = "All";

		public ObservableCollection<SelectableTag> FilterTags { get; } = new();

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
				this.IsBusy = true;			// allow indicator to show
				await Task.Yield();             // yield so UI can render
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

		/// <summary>
		/// Should be called by page on text change.
		/// </summary>
		public void UpdateSearch(string? text)
		{
			this.searchText = text;
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
						Contract? Contract = await Ref.GetContract().ConfigureAwait(false); // TODO FIX
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
				// Enforce single-selection behavior. If tag was not selected, select it and deselect others.
				// If tag was already selected, deselect all.
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

		private async Task LoadContracts()
		{
			try
			{
				IEnumerable<ContractReference> ContractReferences;
				bool Found = false;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						ContractReferences = await Database.Find<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", false),
							new FilterFieldEqualTo("ContractLoaded", true)));
						break;

					case ContractsListMode.ContractTemplates:
						ContractReferences = await Database.Find<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));
						break;

					case ContractsListMode.TokenCreationTemplates:
						ContractReferences = await Database.Find<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						Dictionary<CaseInsensitiveString, bool> ContractIds = [];
						LinkedList<ContractReference> TokenCreationTemplates = [];

						foreach (ContractReference Ref in ContractReferences)
						{
							if (Ref.IsTokenCreationTemplate && Ref.ContractId is not null)
							{
								ContractIds[Ref.ContractId] = true;
								TokenCreationTemplates.AddLast(Ref);
							}
						}

						foreach (string TokenTemplateId in Constants.ContractTemplates.TokenCreationTemplates)
						{
							if (!ContractIds.ContainsKey(TokenTemplateId))
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

									ServiceRef.TagProfile.CheckContractReference(Ref);

									if (Ref.IsTokenCreationTemplate)
									{
										ContractIds[Ref.ContractId] = true;
										TokenCreationTemplates.AddLast(Ref);
									}
								}
							}
						}

						ContractReferences = TokenCreationTemplates;
						break;

					default:
						return;
				}

				// Sort newest first
				List<ContractReference> list = ContractReferences.Where(r => r.ContractId is not null).ToList();
				Found = list.Count > 0;
				list.Sort((a, b) => b.Created.CompareTo(a.Created));

				// Prepare notification map
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory = ServiceRef.NotificationService.GetEventsByCategory(NotificationEventType.Contracts);

				// Reset collections for fresh load
				this.allContracts.Clear();
				this.Contracts.Clear();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.FilterTags.Clear();
					this.tagMap.Clear();
				});

				// Incremental loading: process in batches of 15
				int batchSize = 15;
				int index = 0;

				while (index < list.Count)
				{
					List<ContractReference> batch = list.Skip(index).Take(batchSize).ToList();
					index += batch.Count;

					foreach (ContractReference Ref in batch)
					{
						this.contractsMap[Ref.ContractId] = Ref;

						NotificationEvent[] Events = [];
						if (EventsByCategory.TryGetValue(Ref.ContractId, out NotificationEvent[]? evs))
						{
							// Separate petitions to remain in the service map, only show other events count
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
						this.allContracts.Add(Item);

						// Update tag counts for categories
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
					}

					// Push this batch to UI (respecting search filter)
					this.ApplySearchFilter();

					// Yield to UI
					await Task.Yield();
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

				// Final sort of tags by descending Count (keep "All" first)
				MainThread.BeginInvokeOnMainThread(() =>
				{
					List<SelectableTag> others = this.FilterTags.Where(t => !string.Equals(t.Category, AllCategory, StringComparison.OrdinalIgnoreCase))
						.OrderByDescending(t => t.Count)
						.ToList();
					SelectableTag all = this.FilterTags.FirstOrDefault(t => string.Equals(t.Category, AllCategory, StringComparison.OrdinalIgnoreCase))!;
					this.FilterTags.Clear();
					if (all is not null)
						this.FilterTags.Add(all);
					foreach (SelectableTag t in others)
						this.FilterTags.Add(t);
				});

				this.ShowContractsMissing = !Found;
			}
			finally
			{
				this.IsBusy = false;
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
			public void ToggleSelection() => this.IsSelected = !this.IsSelected; // retained if needed elsewhere
			public void Increment() => this.Count++;
		}
	}
}
