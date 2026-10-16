using System.Text.Json;
using NeuroAccessMaui.Services.Notification;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// A pending request from someone who asks to see one of the user's contracts, listed under "Needs your action".
	/// </summary>
	public class ContractPetitionModel
	{
		private readonly NotificationRecord record;
		private readonly Dictionary<string, string> extras;

		private ContractPetitionModel(NotificationRecord Record, Dictionary<string, string> Extras, string ContractId)
		{
			this.record = Record;
			this.extras = Extras;
			this.ContractId = ContractId;
		}

		/// <summary>
		/// Creates a row from a stored notification, if it is a request to see a contract.
		/// </summary>
		/// <param name="Record">The stored notification.</param>
		/// <returns>The row, or null if the notification isn't a contract request.</returns>
		public static ContractPetitionModel? TryCreate(NotificationRecord Record)
		{
			if (!string.Equals(Record.Action, nameof(NotificationAction.OpenPetition), StringComparison.Ordinal))
				return null;

			Dictionary<string, string>? Extras;

			try
			{
				Extras = JsonSerializer.Deserialize<Dictionary<string, string>>(Record.ExtrasJson);
			}
			catch (JsonException)
			{
				return null;
			}

			if (Extras is null || !Extras.TryGetValue("contractId", out string? ContractId) || string.IsNullOrEmpty(ContractId))
				return null;

			return new ContractPetitionModel(Record, Extras, ContractId);
		}

		/// <summary>
		/// The ID of the stored notification.
		/// </summary>
		public string RecordId => this.record.Id;

		/// <summary>
		/// The petition ID, which identifies the request.
		/// </summary>
		public string? PetitionId => this.extras.TryGetValue("petitionId", out string? Id) ? Id : this.record.CorrelationId;

		/// <summary>
		/// The requested contract.
		/// </summary>
		public string ContractId { get; }

		/// <summary>
		/// Who asks to see which contract.
		/// </summary>
		public string Title => this.record.Title;

		/// <summary>
		/// The purpose the requester gave.
		/// </summary>
		public string Purpose => this.extras.TryGetValue("purpose", out string? Value) && !string.IsNullOrEmpty(Value) ? Value : this.record.Body ?? string.Empty;

		/// <summary>
		/// If there is a purpose to show.
		/// </summary>
		public bool HasPurpose => !string.IsNullOrEmpty(this.Purpose);

		/// <summary>
		/// When the request arrived, as a short local text: a time today, or a date.
		/// </summary>
		public string ReceivedText
		{
			get
			{
				DateTime Created = this.record.TimestampCreated;
				DateTime Local = Created.Kind switch
				{
					DateTimeKind.Local => Created,
					DateTimeKind.Utc => Created.ToLocalTime(),
					_ => DateTime.SpecifyKind(Created, DateTimeKind.Utc).ToLocalTime()
				};

				return Local.Date == DateTime.Today ? Local.ToShortTimeString() : Local.ToShortDateString();
			}
		}

		/// <summary>
		/// Checks if the request matches a search text.
		/// </summary>
		/// <param name="SearchText">The search text.</param>
		/// <returns>If the title, purpose or contract ID contains the text.</returns>
		public bool Matches(string SearchText)
		{
			return this.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
				this.Purpose.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
				this.ContractId.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Builds the intent that opens the request, as a tap on the notification would. The notification isn't consumed,
		/// so the request stays listed until the user answers it.
		/// </summary>
		/// <returns>The notification intent.</returns>
		public NotificationIntent ToIntent()
		{
			return new NotificationIntent()
			{
				Title = this.record.Title,
				Body = this.record.Body,
				Action = NotificationAction.OpenPetition,
				EntityId = this.record.EntityId,
				Channel = this.record.Channel,
				Extras = new Dictionary<string, string>(this.extras),
				Version = this.record.SchemaVersion,
				CorrelationId = this.record.CorrelationId,
				Presentation = this.record.Presentation
			};
		}
	}
}
