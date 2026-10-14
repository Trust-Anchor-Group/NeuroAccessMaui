using System.Security.Cryptography;
using System.Xml;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Routes links to their handlers, prepares them with a timeout and the connectivity they need, and reports failures without exposing link payloads.
	/// </summary>
	public sealed class LinkService : ILinkService
	{
		private static readonly TimeSpan preparationTimeout = TimeSpan.FromSeconds(15);

		private readonly IReadOnlyList<ILinkOpener> handlers;
		private readonly ITagProfile tagProfile;
		private readonly IXmppService xmppService;
		private readonly ILogService logService;
		private readonly IUiService uiService;

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkService"/> class with the application's link handlers.
		/// </summary>
		/// <param name="TagProfile">The profile that decides which application schemes are enabled.</param>
		/// <param name="XmppService">The connection used by handlers that need XMPP.</param>
		/// <param name="LogService">The log that receives preparation failures, without payloads.</param>
		/// <param name="UiService">The UI service used to present errors at the entry point.</param>
		public LinkService(ITagProfile TagProfile, IXmppService XmppService, ILogService LogService, IUiService UiService)
			: this(CreateDefaultHandlers(), TagProfile, XmppService, LogService, UiService)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkService"/> class with an explicit set of link handlers.
		/// </summary>
		/// <param name="Handlers">The link handlers to choose from.</param>
		/// <param name="TagProfile">The profile that decides which application schemes are enabled.</param>
		/// <param name="XmppService">The connection used by handlers that need XMPP.</param>
		/// <param name="LogService">The log that receives preparation failures, without payloads.</param>
		/// <param name="UiService">The UI service used to present errors at the entry point.</param>
		internal LinkService(IEnumerable<ILinkOpener> Handlers, ITagProfile TagProfile, IXmppService XmppService,
			ILogService LogService, IUiService UiService)
		{
			this.handlers = [.. Handlers];
			this.tagProfile = TagProfile;
			this.xmppService = XmppService;
			this.logService = LogService;
			this.uiService = UiService;
		}

		/// <summary>
		/// Gets the schemes that are always accepted when the application opens an external link.
		/// </summary>
		/// <returns>The onboarding and application callback schemes.</returns>
		public static string[] GetBaseSchemes() =>
		[
			Constants.UriSchemes.NeuroAccess,
			Constants.UriSchemes.Onboarding
		];

		/// <inheritdoc/>
		public string[] GetAllowedSchemes()
		{
			List<string> AllowedSchemes = [Constants.UriSchemes.TagSign];

			if (this.tagProfile.LegalIdentity?.HasApprovedPersonalInformation() ?? false)
			{
				AllowedSchemes.Add(Constants.UriSchemes.IotId);
				AllowedSchemes.Add(Constants.UriSchemes.IotSc);
				AllowedSchemes.Add(Constants.UriSchemes.IotDisco);
				AllowedSchemes.Add(Constants.UriSchemes.Xmpp);
				AllowedSchemes.Add(Constants.UriSchemes.EDaler);
				AllowedSchemes.Add(Constants.UriSchemes.NeuroFeature);
			}

			return [.. AllowedSchemes];
		}

		/// <inheritdoc/>
		public Task<PreparationResult<PreparedLink>> PrepareAsync(string Text, IReadOnlyCollection<string>? AllowedSchemes,
			CancellationToken CancellationToken)
		{
			return this.ReadAsync(Text, AllowedSchemes, (Handler, Uri, Token) => Handler.PrepareAsync(Uri, Token), CancellationToken);
		}

		/// <inheritdoc/>
		public Task<PreparationResult<IdentityReference>> ReadIdentityAsync(string Text, CancellationToken CancellationToken)
		{
			return this.ReadAsync(Text, [Constants.UriSchemes.IotId], (Handler, Uri, Token) => Handler is LegalIdentityLink IdentityHandler
				? IdentityHandler.ReadAsync(Uri, Token)
				: Task.FromResult<IdentityReference?>(null), CancellationToken);
		}

		/// <inheritdoc/>
		public Task<PreparationResult<ContractLinkData>> ReadContractAsync(string Text, CancellationToken CancellationToken)
		{
			return this.ReadAsync(Text, [Constants.UriSchemes.IotSc], (Handler, Uri, Token) => Handler is SmartContractLink ContractHandler
				? ContractHandler.ReadAsync(Uri, Token)
				: Task.FromResult<ContractLinkData?>(null), CancellationToken);
		}

		/// <inheritdoc/>
		public Task<PreparationResult<Invitation>> ReadInvitationAsync(string Text, CancellationToken CancellationToken)
		{
			return this.ReadAsync(Text, [Constants.UriSchemes.Onboarding], (Handler, Uri, Token) => Handler is OnboardingLink InvitationHandler
				? InvitationHandler.ReadAsync(Uri, Token)
				: Task.FromResult<Invitation?>(null), CancellationToken);
		}

		/// <inheritdoc/>
		public PreparationResult<IdentityReference> ParseIdentity(string Text)
		{
			if (!TryParseLink(Text, out Uri? Link))
			{
				this.LogFailure("InvalidUri", null);
				return new PreparationResult<IdentityReference>(PreparationStatus.Invalid);
			}

			if (!string.Equals(Link.Scheme, Constants.UriSchemes.IotId, StringComparison.OrdinalIgnoreCase))
			{
				this.LogFailure("SchemeNotAllowed", Link.Scheme);
				return new PreparationResult<IdentityReference>(PreparationStatus.Unsupported);
			}

			string? Identifier = LegalIdentityLink.TryParseIdentifier(Link);
			if (Identifier is null)
			{
				this.LogFailure("HandlerRejected", Link.Scheme, nameof(LegalIdentityLink));
				return new PreparationResult<IdentityReference>(PreparationStatus.Invalid);
			}

			return new PreparationResult<IdentityReference>(PreparationStatus.Ready, new IdentityReference(Identifier, null));
		}

		/// <inheritdoc/>
		public async Task<bool> OpenAsync(string Text, bool ShowErrorIfUnable, IReadOnlyCollection<string>? AllowedSchemes = null,
			bool IsTrustedSource = false)
		{
			if (!TryParseLink(Text, out Uri? Parsed))
			{
				this.LogFailure("InvalidUri", null);
				if (ShowErrorIfUnable)
					await this.DisplayErrorAsync(nameof(AppResources.CodeNotRecognized));
				return false;
			}

			// The scheme is checked on the parsed link, which is also what is routed, so surrounding whitespace or letter case
			// cannot make an application link skip the check. Only application schemes are filtered here; other schemes are
			// limited by the handlers that exist for them.
			string Scheme = Parsed.Scheme;
			AllowedSchemes ??= this.GetAllowedSchemes();
			if (IsApplicationScheme(Scheme) && !AllowedSchemes.Contains(Scheme, StringComparer.OrdinalIgnoreCase) &&
				!GetBaseSchemes().Contains(Scheme, StringComparer.OrdinalIgnoreCase))
			{
				this.LogFailure("SchemeNotAllowed", Scheme);
				if (ShowErrorIfUnable)
					await this.DisplayErrorAsync(nameof(AppResources.UrlSchemaNotPermitted));
				return false;
			}

			// Application callbacks return the user from an external payment page. They are not acted on, since the payment
			// outcome is delivered over XMPP, so an unusable callback is only logged instead of interrupting the user.
			bool IsCallback = string.Equals(Scheme, Constants.UriSchemes.NeuroAccess, StringComparison.OrdinalIgnoreCase);
			bool ShowError = ShowErrorIfUnable && !IsCallback;

			PreparationResult<PreparedLink> Result = await this.ReadAsync(Parsed, null,
				(Handler, Uri, Token) => Handler.PrepareAsync(Uri, Token), CancellationToken.None);

			if (Result.Value is PreparedLink Link)
			{
				if (!IsTrustedSource && !IsApplicationScheme(Scheme) && !await this.ConfirmExternalLinkAsync(Link.Uri))
					return false;

				return await this.ExecuteAsync(Link, ShowError);
			}

			if (ShowError)
			{
				await this.DisplayErrorAsync(Result.Status switch
				{
					PreparationStatus.Unavailable => nameof(AppResources.UnableToConnect),
					PreparationStatus.Unsupported => nameof(AppResources.CodeNotRecognized),
					_ => nameof(AppResources.UnableToOpenLink)
				});
			}

			return false;
		}

		/// <inheritdoc/>
		public async Task<bool> ExecuteAsync(PreparedLink Link, bool ShowErrorIfUnable = true)
		{
			ArgumentNullException.ThrowIfNull(Link);

			try
			{
				await Link.ExecuteAsync();
				return true;
			}
			catch (Exception Ex)
			{
				this.logService.LogWarning("LinkExecutionFailed",
					new KeyValuePair<string, object?>("Scheme", Link.Uri.Scheme),
					new KeyValuePair<string, object?>("ExceptionType", Ex.GetType().Name));
				if (ShowErrorIfUnable)
					await this.DisplayErrorAsync(nameof(AppResources.UnableToOpenLink));
				return false;
			}
		}

		/// <summary>
		/// Parses link text into an absolute URI, after trimming surrounding whitespace.
		/// </summary>
		/// <param name="Text">The link text.</param>
		/// <param name="Link">The parsed link, when the text is a usable absolute URI.</param>
		/// <returns>Whether the text could be parsed.</returns>
		private static bool TryParseLink(string? Text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Uri? Link)
		{
			Link = null;
			string Value = Text?.Trim() ?? string.Empty;
			return Value.Length > 0 && !Value.Any(char.IsControl) && Uri.TryCreate(Value, UriKind.Absolute, out Link);
		}

		/// <summary>
		/// Checks whether a scheme belongs to this application, rather than to the web or another application.
		/// </summary>
		/// <param name="Scheme">The scheme, without the colon.</param>
		/// <returns>Whether the scheme is an application scheme.</returns>
		private static bool IsApplicationScheme(string Scheme)
		{
			return Constants.UriSchemes.GetScheme(Scheme + ":") is not null;
		}

		/// <summary>
		/// Asks the user before a link leaves the application, showing where it goes.
		/// </summary>
		/// <param name="Link">The link that would be opened by the browser or the operating system.</param>
		/// <returns>Whether the user chose to open the link.</returns>
		private Task<bool> ConfirmExternalLinkAsync(Uri Link)
		{
			const int maxDisplayedLength = 200;
			string Target = Link.OriginalString.Length > maxDisplayedLength
				? Link.OriginalString[..maxDisplayedLength] + "…"
				: Link.OriginalString;

			return MainThread.InvokeOnMainThreadAsync(() => this.uiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.OpenExternalLinkTitle)],
				ServiceRef.Localizer[nameof(AppResources.OpenExternalLinkMessage), Target],
				ServiceRef.Localizer[nameof(AppResources.Open)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]));
		}

		private Task<PreparationResult<T>> ReadAsync<T>(string Text, IReadOnlyCollection<string>? AllowedSchemes,
			Func<ILinkOpener, Uri, CancellationToken, Task<T?>> ReadAsync, CancellationToken CancellationToken) where T : class
		{
			if (!TryParseLink(Text, out Uri? Link))
			{
				this.LogFailure("InvalidUri", null);
				return Task.FromResult(new PreparationResult<T>(PreparationStatus.Invalid));
			}

			return this.ReadAsync(Link, AllowedSchemes, ReadAsync, CancellationToken);
		}

		private async Task<PreparationResult<T>> ReadAsync<T>(Uri Link, IReadOnlyCollection<string>? AllowedSchemes,
			Func<ILinkOpener, Uri, CancellationToken, Task<T?>> ReadAsync, CancellationToken CancellationToken) where T : class
		{
			string Scheme = Link.Scheme;
			if (AllowedSchemes is not null && AllowedSchemes.Count > 0 && !AllowedSchemes.Contains(Scheme, StringComparer.OrdinalIgnoreCase))
			{
				this.LogFailure("SchemeNotAllowed", Scheme);
				return new PreparationResult<T>(PreparationStatus.Unsupported);
			}

			ILinkOpener? Handler = this.ResolveHandler(Link);
			if (Handler is null)
			{
				this.LogFailure("HandlerNotFound", Scheme);
				return new PreparationResult<T>(PreparationStatus.Unsupported);
			}

			using CancellationTokenSource Timeout = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
			Timeout.CancelAfter(preparationTimeout);
			CancellationToken Token = Timeout.Token;

			try
			{
				if (Handler.RequiresXmppConnection && !this.xmppService.IsOnline &&
					!await this.xmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect).WaitAsync(Token).ConfigureAwait(false))
				{
					this.LogFailure("NotConnected", Scheme, Handler.GetType().Name);
					return new PreparationResult<T>(PreparationStatus.Unavailable);
				}

				T? Prepared = await ReadAsync(Handler, Link, Token).WaitAsync(Token).ConfigureAwait(false);
				Token.ThrowIfCancellationRequested();

				if (Prepared is null)
				{
					this.LogFailure("HandlerRejected", Scheme, Handler.GetType().Name);
					return new PreparationResult<T>(PreparationStatus.Invalid);
				}

				return new PreparationResult<T>(PreparationStatus.Ready, Prepared);
			}
			catch (OperationCanceledException)
			{
				bool Cancelled = CancellationToken.IsCancellationRequested;
				this.LogFailure(Cancelled ? "Cancelled" : "TimedOut", Scheme, Handler.GetType().Name);
				return new PreparationResult<T>(Cancelled ? PreparationStatus.Cancelled : PreparationStatus.Unavailable);
			}
			catch (Exception Ex) when (Ex is FormatException or CryptographicException or XmlException or
				ItemNotFoundException or BadRequestException)
			{
				this.LogFailure("InvalidPayload", Scheme, Handler.GetType().Name, Ex.GetType().Name);
				return new PreparationResult<T>(PreparationStatus.Invalid);
			}
			catch (Exception Ex)
			{
				// Exception messages can contain parts of the original link, such as invitation keys, so only the type is logged.
				this.LogFailure("Unavailable", Scheme, Handler.GetType().Name, Ex.GetType().Name);
				return new PreparationResult<T>(PreparationStatus.Unavailable);
			}
		}

		private ILinkOpener? ResolveHandler(Uri Uri)
		{
			ILinkOpener? Best = null;
			Grade BestGrade = Grade.NotAtAll;

			foreach (ILinkOpener Handler in this.handlers)
			{
				Grade HandlerGrade = Handler.Supports(Uri);
				if (HandlerGrade > BestGrade)
				{
					Best = Handler;
					BestGrade = HandlerGrade;
				}
			}

			return Best;
		}

		private static ILinkOpener[] CreateDefaultHandlers() =>
		[
			new NeuroAccessLink(),
			new LegalIdentityLink(),
			new SmartContractLink(),
			new TagSignatureLink(),
			new ThingsDiscoveryLink(),
			new EDalerLink(),
			new NeuroFeatureLink(),
			new OnboardingLink(),
			new XmppLink(),
			new BrowserLink(),
			new DefaultLink()
		];

		private void LogFailure(string Reason, string? Scheme, string? Handler = null, string? ExceptionType = null)
		{
			this.logService.LogWarning("LinkPreparationFailed",
				new KeyValuePair<string, object?>("Reason", Reason),
				new KeyValuePair<string, object?>("Scheme", Scheme ?? string.Empty),
				new KeyValuePair<string, object?>("Handler", Handler ?? string.Empty),
				new KeyValuePair<string, object?>("ExceptionType", ExceptionType ?? string.Empty));
		}

		private Task DisplayErrorAsync(string MessageKey)
		{
			return MainThread.InvokeOnMainThreadAsync(() => this.uiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				ServiceRef.Localizer[MessageKey]));
		}
	}
}
