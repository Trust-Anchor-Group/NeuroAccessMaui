using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using NeuroFeatures;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Remembers token names, so notifications about a token can show its name.
	/// </summary>
	public sealed class TokenNameCache
	{
		private readonly ConcurrentDictionary<string, string> names = new(StringComparer.OrdinalIgnoreCase);
		private readonly ConcurrentDictionary<string, Task<string?>> pending = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Remembers the name of a token.
		/// </summary>
		/// <param name="Token">The token, if any.</param>
		public void Remember(Token? Token)
		{
			if (Token is null || string.IsNullOrEmpty(Token.TokenId) || string.IsNullOrWhiteSpace(Token.FriendlyName))
				return;

			this.names[Token.TokenId] = Token.FriendlyName;
		}

		/// <summary>
		/// Gets the remembered name of a token.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <param name="Name">The name, if remembered.</param>
		/// <returns>If a name is remembered for the token.</returns>
		public bool TryGetName(string TokenId, [NotNullWhen(true)] out string? Name)
		{
			return this.names.TryGetValue(TokenId, out Name);
		}

		/// <summary>
		/// Gets the name of a token. A name that isn't remembered is fetched from the token's Neuron, once even when
		/// several callers ask at the same time.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <returns>The name, or null if the token couldn't be fetched.</returns>
		public async Task<string?> GetNameAsync(string TokenId)
		{
			if (this.names.TryGetValue(TokenId, out string? Name))
				return Name;

			Task<string?> Fetch = this.pending.GetOrAdd(TokenId, Id => Task.Run(() => this.FetchNameAsync(Id)));

			try
			{
				return await Fetch;
			}
			finally
			{
				this.pending.TryRemove(new KeyValuePair<string, Task<string?>>(TokenId, Fetch));
			}
		}

		private async Task<string?> FetchNameAsync(string TokenId)
		{
			try
			{
				Token Token = await ServiceRef.XmppService.GetNeuroFeature(TokenId);
				this.Remember(Token);

				return string.IsNullOrWhiteSpace(Token.FriendlyName) ? null : Token.FriendlyName;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}
	}
}
