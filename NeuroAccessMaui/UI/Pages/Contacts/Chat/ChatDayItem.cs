using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// The day the messages below it were written.
	/// </summary>
	public sealed class ChatDayItem : ChatItem
	{
		/// <summary>
		/// The day the messages below it were written.
		/// </summary>
		/// <param name="Day">The day, in local time.</param>
		public ChatDayItem(DateTime Day)
			: base(Day.Date)
		{
			this.Text = GetText(Day.Date);
		}

		/// <summary>
		/// The day, as shown: today, yesterday, a weekday in the last week, or a date.
		/// </summary>
		public string Text { get; }

		private static string GetText(DateTime Day)
		{
			DateTime Today = DateTime.Today;
			CultureInfo Culture = CultureInfo.CurrentCulture;

			if (Day == Today)
				return ServiceRef.Localizer[nameof(AppResources.Today)];

			if (Day == Today.AddDays(-1))
				return ServiceRef.Localizer[nameof(AppResources.Yesterday)];

			string Text;

			if (Day > Today.AddDays(-7) && Day < Today)
				Text = Day.ToString("dddd", Culture);
			else if (Day.Year == Today.Year)
				Text = Day.ToString("M", Culture);
			else
				Text = Day.ToString("D", Culture);

			return string.IsNullOrEmpty(Text) ? Text : char.ToUpper(Text[0], Culture) + Text[1..];
		}
	}
}
