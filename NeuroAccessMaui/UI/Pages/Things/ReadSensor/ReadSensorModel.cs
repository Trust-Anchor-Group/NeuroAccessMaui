using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using NeuroAccessMaui.UI.Pages.Things.ViewThing;
using IdApp.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.PEP;
using Waher.Networking.XMPP.Sensor;
using Waher.Things;
using Waher.Things.SensorData;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ReadSensorModel : XmppViewModel
	{
		private ContactInfo thing;
		private ThingReference thingRef;
		private SensorDataClientRequest request;

		/// <summary>
		/// Creates an instance of the <see cref="ReadSensorModel"/> class.
		/// </summary>
		protected internal ReadSensorModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.LabelClicked(x));

			this.SensorData = new ObservableCollection<object>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out ViewThingNavigationArgs args))
			{
				this.thing = args.Thing;

				if (string.IsNullOrEmpty(this.thing.NodeId) && string.IsNullOrEmpty(this.thing.SourceId) && string.IsNullOrEmpty(this.thing.Partition))
					this.thingRef = null;
				else
					this.thingRef = new ThingReference(this.thing.NodeId, this.thing.SourceId, this.thing.Partition);

				if (this.thing.MetaData is not null && this.thing.MetaData.Length > 0)
				{
					this.SensorData.Add(new HeaderModel(LocalizationResourceManager.Current["GeneralInformation"]));

					foreach (Property Tag in this.thing.MetaData)
						this.SensorData.Add(new HumanReadableTag(Tag));
				}

				this.SupportsSensorEvents = this.thing.SupportsSensorEvents ?? false;
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.XmppService.OnPresence += this.Xmpp_OnPresence;
			this.TagProfile.Changed += this.TagProfile_Changed;

			if (this.thingRef is null)
				this.request = this.XmppService.RequestSensorReadout(this.GetFullJid(), FieldType.All);
			else
				this.request = this.XmppService.RequestSensorReadout(this.GetFullJid(), new ThingReference[] { this.thingRef }, FieldType.All);

			this.request.OnStateChanged += this.Request_OnStateChanged;
			this.request.OnFieldsReceived += this.Request_OnFieldsReceived;
			this.request.OnErrorsReceived += this.Request_OnErrorsReceived;

			this.XmppService.RegisterPepHandler(typeof(SensorData), this.SensorDataPersonalEventHandler);
		}

		private Task Request_OnStateChanged(object Sender, SensorDataReadoutState NewState)
		{
			this.Status = NewState switch
			{
				SensorDataReadoutState.Requested => LocalizationResourceManager.Current["SensorDataRequested"],
				SensorDataReadoutState.Accepted => LocalizationResourceManager.Current["SensorDataAccepted"],
				SensorDataReadoutState.Cancelled => LocalizationResourceManager.Current["SensorDataCancelled"],
				SensorDataReadoutState.Done => LocalizationResourceManager.Current["SensorDataDone"],
				SensorDataReadoutState.Failure => LocalizationResourceManager.Current["SensorDataFailure"],
				SensorDataReadoutState.Receiving => LocalizationResourceManager.Current["SensorDataReceiving"],
				SensorDataReadoutState.Started => LocalizationResourceManager.Current["SensorDataStarted"],
				_ => string.Empty,
			};

			return Task.CompletedTask;
		}

		private string GetFieldTypeString(FieldType Type)
		{
			if (Type.HasFlag(FieldType.Identity))
				return LocalizationResourceManager.Current["SensorDataHeaderIdentity"];
			else if (Type.HasFlag(FieldType.Status))
				return LocalizationResourceManager.Current["SensorDataHeaderStatus"];
			else if (Type.HasFlag(FieldType.Momentary))
				return LocalizationResourceManager.Current["SensorDataHeaderMomentary"];
			else if (Type.HasFlag(FieldType.Peak))
				return LocalizationResourceManager.Current["SensorDataHeaderPeak"];
			else if (Type.HasFlag(FieldType.Computed))
				return LocalizationResourceManager.Current["SensorDataHeaderComputed"];
			else if (Type.HasFlag(FieldType.Historical))
				return LocalizationResourceManager.Current["SensorDataHeaderHistorical"];
			else
				return LocalizationResourceManager.Current["SensorDataHeaderOther"];
		}

		private Task Request_OnFieldsReceived(object Sender, IEnumerable<Field> NewFields)
		{
			return this.NewFieldsReported(NewFields);
		}

		private Task NewFieldsReported(IEnumerable<Field> NewFields)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				string Category;
				HeaderModel CategoryHeader = null;
				string FieldName;
				int CategoryIndex = 0;
				int i, j, c;
				bool IsMin;
				bool IsMax;

				foreach (Field Field in NewFields)
				{
					FieldName = Field.Name;

					if (Field.Type.HasFlag(FieldType.Historical))
					{
						IsMin = false;
						IsMax = false;

						if (FieldName.EndsWith(", Min", StringComparison.InvariantCultureIgnoreCase))
						{
							FieldName = FieldName[0..^5];
							IsMin = true;
						}
						else if (FieldName.EndsWith(", Max", StringComparison.InvariantCultureIgnoreCase))
						{
							FieldName = FieldName[0..^5];
							IsMax = true;
						}

						Category = FieldName;
					}
					else
					{
						Category = this.GetFieldTypeString(Field.Type);
						IsMin = IsMax = false;
					}

					if (CategoryHeader is null || CategoryHeader.Label != Category)
					{
						CategoryHeader = null;
						CategoryIndex = 0;

						foreach (object Item in this.SensorData)
						{
							if (Item is HeaderModel Header && Header.Label == Category)
							{
								CategoryHeader = Header;
								break;
							}
							else
								CategoryIndex++;
						}

						if (CategoryHeader is null)
						{
							CategoryHeader = new HeaderModel(Category);
							this.SensorData.Add(CategoryHeader);
						}
					}

					if (Field.Type.HasFlag(FieldType.Historical))
					{
						for (i = CategoryIndex + 1, c = this.SensorData.Count; i < c; i++)
						{
							object Obj = this.SensorData[i];

							if (Obj is GraphModel GraphModel)
							{
								j = string.Compare(FieldName, GraphModel.FieldName, true);
								if (j < 0)
									continue;
								else
								{
									if (j == 0)
									{
										if (IsMin)
											GraphModel.AddMin(Field);
										else if (IsMax)
											GraphModel.AddMax(Field);
										else
											GraphModel.Add(Field);
									}
									else if (j > 0 && !IsMin && !IsMax)
										this.SensorData.Insert(i, new GraphModel(Field, this));

									break;
								}
							}
							else
							{
								this.SensorData.Insert(i, new GraphModel(Field, this));
								break;
							}
						}

						if (i >= c)
							this.SensorData.Add(new GraphModel(Field, this));
					}
					else
					{
						for (i = CategoryIndex + 1, c = this.SensorData.Count; i < c; i++)
						{
							object Obj = this.SensorData[i];

							if (Obj is FieldModel FieldModel)
							{
								j = string.Compare(FieldName, FieldModel.Name, true);
								if (j < 0)
									continue;
								else
								{
									if (j == 0)
										FieldModel.Field = Field;
									else if (j > 0)
										this.SensorData.Insert(i, new FieldModel(Field));

									break;
								}
							}
							else
							{
								this.SensorData.Insert(i, new FieldModel(Field));
								break;
							}
						}

						if (i >= c)
							this.SensorData.Add(new FieldModel(Field));
					}
				}
			});

			return Task.CompletedTask;
		}

		private Task Request_OnErrorsReceived(object Sender, IEnumerable<ThingError> NewErrors)
		{
			return this.NewErrorsReported(NewErrors);
		}

		private Task NewErrorsReported(IEnumerable<ThingError> NewErrors)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				string Errors = LocalizationResourceManager.Current["Errors"];
				HeaderModel CategoryHeader = null;
				int CategoryIndex = 0;
				int i, c;

				foreach (object Item in this.SensorData)
				{
					if (Item is HeaderModel Header && Header.Label == Errors)
					{
						CategoryHeader = Header;
						break;
					}
					else
						CategoryIndex++;
				}

				if (CategoryHeader is null)
				{
					CategoryHeader = new HeaderModel(Errors);
					this.SensorData.Add(CategoryHeader);
				}

				for (i = CategoryIndex + 1, c = this.SensorData.Count; i < c; i++)
				{
					object Obj = this.SensorData[i];

					if (Obj is ErrorModel ErrorModel)
						continue;
					else
					{
						foreach (ThingError Error in NewErrors)
						{
							this.SensorData.Insert(i++, new ErrorModel(Error));
							c++;
						}

						break;
					}
				}

				if (i >= c)
				{
					foreach (ThingError Error in NewErrors)
						this.SensorData.Add(new ErrorModel(Error));
				}
			});

			return Task.CompletedTask;
		}

		private async Task SensorDataPersonalEventHandler(object Sender, PersonalEventNotificationEventArgs e)
		{
			if (e.PersonalEvent is SensorData SensorData &&
				string.Compare(this.thing.BareJid, e.Publisher, true) == 0 &&
				string.IsNullOrEmpty(this.thing.SourceId) &&
				string.IsNullOrEmpty(this.thing.NodeId) &&
				string.IsNullOrEmpty(this.thing.Partition))
			{
				if (SensorData.Fields is not null)
					await this.NewFieldsReported(SensorData.Fields);

				if (SensorData.Errors is not null)
					await this.NewErrorsReported(SensorData.Errors);
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.UnregisterPepHandler(typeof(SensorData), this.SensorDataPersonalEventHandler);

			this.XmppService.OnPresence -= this.Xmpp_OnPresence;
			this.TagProfile.Changed -= this.TagProfile_Changed;

			if (this.request is not null &&
				(this.request.State == SensorDataReadoutState.Receiving ||
				this.request.State == SensorDataReadoutState.Accepted ||
				this.request.State == SensorDataReadoutState.Requested ||
				this.request.State == SensorDataReadoutState.Started))
			{
				await this.request.Cancel();
			}

			await base.OnDispose();
		}

		private Task Xmpp_OnPresence(object Sender, PresenceEventArgs e)
		{
			this.CalcThingIsOnline();
			return Task.CompletedTask;
		}

		private void CalcThingIsOnline()
		{
			if (this.thing is null)
				this.IsThingOnline = false;
			else
			{
				RosterItem Item = this.XmppService.GetRosterItem(this.thing.BareJid);
				this.IsThingOnline = Item is not null && Item.HasLastPresence && Item.LastPresence.IsOnline;
			}
		}

		private string GetFullJid()
		{
			if (this.thing is null)
				return null;
			else
			{
				RosterItem Item = this.XmppService.GetRosterItem(this.thing.BareJid);

				if (Item is null || !Item.HasLastPresence || !Item.LastPresence.IsOnline)
					return null;
				else
					return Item.LastPresenceFullJid;
			}
		}

		private void AssignProperties()
		{
			this.CalcThingIsOnline();
		}

		private void EvaluateAllCommands()
		{
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
			});

			return Task.CompletedTask;
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
		}

		#region Properties

		/// <summary>
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<object> SensorData { get; }

		/// <summary>
		/// See <see cref="IsThingOnline"/>
		/// </summary>
		public static readonly BindableProperty IsThingOnlineProperty =
			BindableProperty.Create(nameof(IsThingOnline), typeof(bool), typeof(ReadSensorModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsThingOnline
		{
			get => (bool)this.GetValue(IsThingOnlineProperty);
			set => this.SetValue(IsThingOnlineProperty, value);
		}

		/// <summary>
		/// See <see cref="SupportsSensorEvents"/>
		/// </summary>
		public static readonly BindableProperty SupportsSensorEventsProperty =
			BindableProperty.Create(nameof(SupportsSensorEvents), typeof(bool), typeof(ReadSensorModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool SupportsSensorEvents
		{
			get => (bool)this.GetValue(SupportsSensorEventsProperty);
			set => this.SetValue(SupportsSensorEventsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasStatus"/>
		/// </summary>
		public static readonly BindableProperty HasStatusProperty =
			BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ReadSensorModel), default(bool));

		/// <summary>
		/// Gets or sets whether there's a status message to display
		/// </summary>
		public bool HasStatus
		{
			get => (bool)this.GetValue(HasStatusProperty);
			set => this.SetValue(HasStatusProperty, value);
		}

		/// <summary>
		/// See <see cref="Status"/>
		/// </summary>
		public static readonly BindableProperty StatusProperty =
			BindableProperty.Create(nameof(Status), typeof(string), typeof(ReadSensorModel), default(string));

		/// <summary>
		/// Gets or sets a status message.
		/// </summary>
		public string Status
		{
			get => (string)this.GetValue(StatusProperty);
			set
			{
				this.SetValue(StatusProperty, value);
				this.HasStatus = !string.IsNullOrEmpty(value);
			}
		}

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public System.Windows.Input.ICommand ClickCommand { get; }

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this);
			else if (obj is FieldModel Field)
				return ViewClaimThingViewModel.LabelClicked(Field.Name, Field.ValueString, Field.ValueString, this);
			else if (obj is ErrorModel Error)
				return ViewClaimThingViewModel.LabelClicked(string.Empty, Error.ErrorMessage, Error.ErrorMessage, this);
			else if (obj is string s)
				return ViewClaimThingViewModel.LabelClicked(string.Empty, s, s, this);
			else
				return Task.CompletedTask;
		}

	}
}
