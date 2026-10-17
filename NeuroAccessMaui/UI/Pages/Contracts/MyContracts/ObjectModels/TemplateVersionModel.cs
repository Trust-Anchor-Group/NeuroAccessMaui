using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// One version of a template in the template picker, listed under the template when its versions are shown.
	/// </summary>
	public class TemplateVersionModel
	{
		private readonly DateTime created;

		/// <summary>
		/// Creates a version of a template.
		/// </summary>
		/// <param name="Contract">The template.</param>
		/// <param name="IsNewest">If this is the newest version.</param>
		public TemplateVersionModel(ContractModel Contract, bool IsNewest)
		{
			this.Contract = Contract;
			this.IsNewest = IsNewest;
			this.created = ToLocalTime(Contract.Timestamp);
		}

		/// <summary>
		/// The template.
		/// </summary>
		public ContractModel Contract { get; }

		/// <summary>
		/// If this is the newest version.
		/// </summary>
		public bool IsNewest { get; }

		/// <summary>
		/// If a divider is drawn above the version. Every version but the first has one.
		/// </summary>
		public bool ShowsDivider => !this.IsNewest;

		/// <summary>
		/// The date the version was created, in local time.
		/// </summary>
		public string DateText => this.created == DateTime.MinValue ? string.Empty : this.created.ToShortDateString();

		/// <summary>
		/// The time of day the version was created, in local time.
		/// </summary>
		public string TimeText => this.created == DateTime.MinValue ? string.Empty : this.created.ToShortTimeString();

		/// <summary>
		/// What a screen reader announces for the version.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				string Text = ServiceRef.Localizer[nameof(AppResources.TemplateUseVersion), this.DateText + " " + this.TimeText];

				if (this.IsNewest)
					Text += ", " + ServiceRef.Localizer[nameof(AppResources.TemplateVersionNewest)];

				return Text;
			}
		}

		/// <summary>
		/// Converts a stored time to local time. Times without a kind are stored in UTC.
		/// </summary>
		/// <param name="Time">The stored time.</param>
		/// <returns>The local time, or <see cref="DateTime.MinValue"/> if no time is known.</returns>
		private static DateTime ToLocalTime(DateTime Time)
		{
			if (Time == DateTime.MinValue)
				return DateTime.MinValue;

			return Time.Kind switch
			{
				DateTimeKind.Local => Time,
				DateTimeKind.Utc => Time.ToLocalTime(),
				_ => DateTime.SpecifyKind(Time, DateTimeKind.Utc).ToLocalTime()
			};
		}
	}
}
