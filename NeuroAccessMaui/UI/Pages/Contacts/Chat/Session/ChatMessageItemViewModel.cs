using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Chat.Models;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Session
{
	/// <summary>
	/// Represents a chat message prepared for UI binding.
	/// </summary>
	public partial class ChatMessageItemViewModel : ObservableObject
	{
		private readonly ChatMessageDescriptor descriptor;
		private readonly ChatRenderResult renderResult;
		private readonly string displayText;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMessageItemViewModel"/> class.
		/// </summary>
		/// <param name="Descriptor">Source descriptor.</param>
		/// <param name="RenderResult">Rendered result.</param>
		public ChatMessageItemViewModel(ChatMessageDescriptor Descriptor, ChatRenderResult RenderResult)
		{
			this.descriptor = Descriptor ?? throw new ArgumentNullException(nameof(Descriptor));
			this.renderResult = RenderResult ?? throw new ArgumentNullException(nameof(RenderResult));
			this.deliveryStatus = this.descriptor.DeliveryStatus;
			this.displayText = this.CalculateDisplayText();
		}

		/// <summary>
		/// Gets the message identifier.
		/// </summary>
		public string MessageId => this.descriptor.MessageId;

		/// <summary>
		/// Gets the message direction.
		/// </summary>
		public ChatMessageDirection Direction => this.descriptor.Direction;

		/// <summary>
		/// Gets the creation timestamp.
		/// </summary>
		public DateTime Created => this.descriptor.Created;

		/// <summary>
		/// Gets the creation timestamp in local time
		/// </summary>
		public DateTime CreatedLocal => this.descriptor.Created.ToLocalTime();

		/// <summary>
		/// Gets the update timestamp.
		/// </summary>
		public DateTime Updated => this.descriptor.Updated == DateTime.MinValue ? this.descriptor.Created : this.descriptor.Updated;

		/// <summary>
		/// Gets the update timestamp in local time.
		/// </summary>
		public DateTime UpdatedLocal => this.Updated.ToLocalTime();

		/// <summary>
		/// Gets whether the message was edited.
		/// </summary>
		public bool IsEdited => this.descriptor.IsEdited;

		/// <summary>
		/// Gets a display caption describing the edit timestamp.
		/// </summary>
		public string EditedCaption
		{
			get
			{
				if (!this.IsEdited)
					return string.Empty;

				string timeText = this.UpdatedLocal.ToString("t", CultureInfo.CurrentCulture);
				return string.Format(CultureInfo.CurrentCulture, AppResources.ChatEditedTimestamp, timeText);
			}
		}

		/// <summary>
		/// Gets the render segments.
		/// </summary>
		public IReadOnlyList<ChatRenderSegment> Segments => this.renderResult.Segments;

		/// <summary>
		/// Gets the underlying descriptor.
		/// </summary>
		public ChatMessageDescriptor Descriptor => this.descriptor;

		/// <summary>
		/// Gets the render result.
		/// </summary>
		public ChatRenderResult RenderResult => this.renderResult;

		/// <summary>
		/// Gets a plain text representation suitable for simple text templates.
		/// </summary>
		public string DisplayText => this.displayText;

		/// <summary>
		/// Delivery status for the message.
		/// </summary>
		[ObservableProperty]
		private ChatDeliveryStatus deliveryStatus;

		/// <summary>
		/// Updates the delivery status.
		/// </summary>
		/// <param name="Status">New status.</param>
		public void UpdateDeliveryStatus(ChatDeliveryStatus Status)
		{
			this.DeliveryStatus = Status;
		}

		private string CalculateDisplayText()
		{
			if (this.renderResult.Segments is null || this.renderResult.Segments.Count == 0)
				return this.descriptor.PlainText ?? string.Empty;

			foreach (ChatRenderSegment Segment in this.renderResult.Segments)
			{
				if (Segment is null)
					continue;

				if (Segment.Attributes is not null &&
					Segment.Attributes.TryGetValue("format", out string? Format) &&
					string.Equals(Format, "plain", StringComparison.OrdinalIgnoreCase))
				{
					if (!string.IsNullOrWhiteSpace(Segment.Value))
						return Segment.Value;
				}
			}

			ChatRenderSegment First = this.renderResult.Segments[0];
			if (!string.IsNullOrWhiteSpace(First.Value))
				return First.Value;

			if (!string.IsNullOrWhiteSpace(this.descriptor.PlainText))
				return this.descriptor.PlainText;

			if (!string.IsNullOrWhiteSpace(this.descriptor.Markdown))
				return this.descriptor.Markdown;

			return this.descriptor.Html ?? string.Empty;
		}
	}
}
