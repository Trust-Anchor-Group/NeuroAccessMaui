using Waher.Content.Emoji;

namespace NeuroAccessMaui.Services.Data
{
	/// <summary>
	/// Representation of an ISO3166-1 Country
	/// </summary>
	public class ISO3166Country(string Name, string Alpha2, string Alpha3,
		int NumericCode, string DialCode, EmojiInfo? EmojiInfo = null)
	{
		public string Name { get; private set; } = Name;
		public string Alpha2 { get; private set; } = Alpha2;
		public string Alpha3 { get; private set; } = Alpha3;
		public int NumericCode { get; private set; } = NumericCode;
		public string DialCode { get; private set; } = DialCode;
		public EmojiInfo EmojiInfo { get; set; } = EmojiInfo ?? EmojiUtilities.Emoji_waving_white_flag;
	}
}
