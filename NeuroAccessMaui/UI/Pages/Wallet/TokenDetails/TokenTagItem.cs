namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A tag the token's creator attached to the token, or its reference. Web addresses can be opened, and every value
	/// can be copied.
	/// </summary>
	/// <param name="Name">Name of the tag.</param>
	/// <param name="Value">Value of the tag.</param>
	public class TokenTagItem(string Name, string Value)
		: TokenPropertyItem(Name, Value, IsWebLink(Value) ? TokenPropertyKind.Link : TokenPropertyKind.Text)
	{
		/// <summary>
		/// If the value is a web address that can be opened.
		/// </summary>
		public bool IsLink => this.Kind == TokenPropertyKind.Link;

		/// <summary>
		/// Checks if a value is an http or https address.
		/// </summary>
		/// <param name="Value">The value.</param>
		/// <returns>If the value is a web address.</returns>
		public static bool IsWebLink(string? Value)
		{
			return Uri.TryCreate(Value, UriKind.Absolute, out Uri? Parsed) &&
				(Parsed.Scheme == Uri.UriSchemeHttp || Parsed.Scheme == Uri.UriSchemeHttps);
		}
	}
}
