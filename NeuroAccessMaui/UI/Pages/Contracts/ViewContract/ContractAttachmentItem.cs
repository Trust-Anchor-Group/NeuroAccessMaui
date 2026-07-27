using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Photos;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// Presents privacy-conscious metadata and loading state for one contract attachment.
	/// </summary>
	public partial class ContractAttachmentItem : ObservableObject
	{
		/// <summary>
		/// Initializes a contract attachment summary.
		/// </summary>
		/// <param name="Attachment">Protocol attachment metadata.</param>
		/// <param name="Index">One-based attachment position within the agreement.</param>
		public ContractAttachmentItem(Attachment Attachment, int Index)
		{
			this.Attachment = Attachment;
			this.Label = string.Format(
				System.Globalization.CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.ContractAttachmentLabelFormat)],
				Index);
			this.IsImage = PhotosLoader.IsSupportedImageContentType(
				Attachment.ContentType);
			this.TypeText = this.IsImage
				? ServiceRef.Localizer[nameof(AppResources.ContractAttachmentImage)]
				: ServiceRef.Localizer[nameof(AppResources.ContractAttachmentFile)];
			this.CanOpen = !string.IsNullOrWhiteSpace(Attachment.Url);
			if (!this.CanOpen)
			{
				this.ErrorText =
					ServiceRef.Localizer[nameof(AppResources.ContractAttachmentUnavailable)];
			}
		}

		/// <summary>
		/// Gets the generic label that avoids exposing a potentially sensitive filename.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the localized broad attachment type.
		/// </summary>
		public string TypeText { get; }

		/// <summary>
		/// Gets whether the attachment uses a supported image content type.
		/// </summary>
		public bool IsImage { get; }

		/// <summary>
		/// Gets whether sufficient metadata exists to request the attachment.
		/// </summary>
		public bool CanOpen { get; }

		/// <summary>
		/// Gets whether the attachment can currently be requested.
		/// </summary>
		public bool CanRequestOpen => this.CanOpen && !this.IsLoading;

		/// <summary>
		/// Gets the protocol attachment metadata used only after an explicit open action.
		/// </summary>
		internal Attachment Attachment { get; }

		/// <summary>
		/// Gets or sets whether the attachment is being retrieved.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanRequestOpen))]
		private bool isLoading;

		/// <summary>
		/// Gets or sets the localized failure shown for this attachment.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasError))]
		private string errorText = string.Empty;

		/// <summary>
		/// Gets whether the attachment has a user-visible failure.
		/// </summary>
		public bool HasError => !string.IsNullOrWhiteSpace(this.ErrorText);
	}
}
