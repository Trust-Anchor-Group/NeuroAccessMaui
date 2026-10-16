using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Defines how a KYC process submits identity applications.
	/// </summary>
	public class KycApplicationPolicy
	{
		/// <summary>
		/// Gets or sets the identity application mode.
		/// </summary>
		public KycApplicationMode Mode { get; set; } = KycApplicationMode.Direct;

		/// <summary>
		/// Gets or sets the content routing policy for application submission.
		/// </summary>
		public KycContentPolicy Content { get; set; } = new KycContentPolicy();
	}

	/// <summary>
	/// Defines the supported identity application submission modes.
	/// </summary>
	public enum KycApplicationMode
	{
		/// <summary>
		/// Submit one non-preview identity application.
		/// </summary>
		Direct,

		/// <summary>
		/// Submit a preview identity first, then promote retained content into a final application.
		/// </summary>
		Preview
	}

	/// <summary>
	/// Defines which identity content is included in the first application and retained for promotion.
	/// </summary>
	public class KycContentPolicy
	{
		/// <summary>
		/// Gets the property keys sent with the first application only.
		/// </summary>
		public Collection<string> IncludePropertyKeys { get; } = new Collection<string>();

		/// <summary>
		/// Gets the property keys sent with the first application and retained for promotion.
		/// </summary>
		public Collection<string> KeepPropertyKeys { get; } = new Collection<string>();

		/// <summary>
		/// Gets the retained property keys the user may leave out of the final application, mapped to whether they are retained by default.
		/// </summary>
		/// <remarks>
		/// Every key here is also listed in <see cref="KeepPropertyKeys"/>. Keys not listed here are always retained.
		/// </remarks>
		public Dictionary<string, bool> OptionalKeepDefaults { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the attachment names sent with the first application only.
		/// </summary>
		public Collection<string> IncludeAttachmentNames { get; } = new Collection<string>();

		/// <summary>
		/// Gets the attachment tags sent with the first application only.
		/// </summary>
		public Collection<string> IncludeAttachmentTags { get; } = new Collection<string>();

		/// <summary>
		/// Gets the attachment names sent with the first application and retained for promotion.
		/// </summary>
		public Collection<string> KeepAttachmentNames { get; } = new Collection<string>();

		/// <summary>
		/// Gets the attachment tags sent with the first application and retained for promotion.
		/// </summary>
		public Collection<string> KeepAttachmentTags { get; } = new Collection<string>();
	}
}
