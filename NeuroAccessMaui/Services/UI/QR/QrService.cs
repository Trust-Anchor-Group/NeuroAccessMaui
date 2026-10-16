using NeuroAccessMaui.Links;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.UI.Pages.Main.QR;

namespace NeuroAccessMaui.Services.UI.QR
{
	/// <summary>
	/// Scans codes through the scanner page, preparing their content before the page closes, and generates QR code images.
	/// </summary>
	public sealed class QrService : IQrService
	{
		private readonly INavigationService navigationService;
		private readonly ILinkService linkService;
		private readonly ILogService logService;

		/// <summary>
		/// Initializes a new instance of the <see cref="QrService"/> class.
		/// </summary>
		/// <param name="NavigationService">Navigation used to present the scanner.</param>
		/// <param name="LinkService">Link reading, preparation, and execution.</param>
		/// <param name="LogService">The log that receives scanner navigation failures.</param>
		public QrService(INavigationService NavigationService, ILinkService LinkService, ILogService LogService)
		{
			this.navigationService = NavigationService;
			this.linkService = LinkService;
			this.logService = LogService;
		}

		/// <inheritdoc/>
		public Task<string?> ScanAsync(string? Title, string[] AllowedSchemes)
		{
			return this.ScanCoreAsync(Title, AllowedSchemes, null);
		}

		/// <inheritdoc/>
		public async Task<T?> ScanAsync<T>(string? Title, string[] AllowedSchemes,
			Func<string, CancellationToken, Task<PreparationResult<T>>> PrepareAsync) where T : class
		{
			ArgumentNullException.ThrowIfNull(PrepareAsync);

			string? PreparedText = null;
			T? Prepared = null;
			string? Accepted = await this.ScanCoreAsync(Title, AllowedSchemes, async (Text, Token) =>
			{
				PreparationResult<T> Result = await PrepareAsync(Text, Token);
				if (Result.Value is T Value && !Token.IsCancellationRequested)
				{
					PreparedText = Text;
					Prepared = Value;
				}
				return Result.Status;
			});

			// The value is returned only when it was prepared from the text the scanner accepted.
			return Accepted is not null && string.Equals(Accepted, PreparedText, StringComparison.Ordinal) ? Prepared : null;
		}

		/// <inheritdoc/>
		public Task<IdentityReference?> ScanIdentityAsync(string Title, bool LookUpIdentity = true)
		{
			if (LookUpIdentity)
				return this.ScanAsync(Title, [Constants.UriSchemes.IotId], this.linkService.ReadIdentityAsync);

			return this.ScanAsync(Title, [Constants.UriSchemes.IotId],
				(Text, _) => Task.FromResult(this.linkService.ParseIdentity(Text)));
		}

		/// <inheritdoc/>
		public Task<ContractLinkData?> ScanContractAsync(string Title)
		{
			return this.ScanAsync(Title, [Constants.UriSchemes.IotSc], this.linkService.ReadContractAsync);
		}

		/// <inheritdoc/>
		public Task<Invitation?> ScanInvitationAsync(string Title)
		{
			return this.ScanAsync(Title, [Constants.UriSchemes.Onboarding], this.linkService.ReadInvitationAsync);
		}

		/// <inheritdoc/>
		public async Task ScanAndOpenAsync(string Title, string[]? AllowedSchemes = null)
		{
			string[] Schemes = AllowedSchemes ?? this.linkService.GetAllowedSchemes();
			PreparedLink? Link = await this.ScanAsync(Title, Schemes,
				(Text, Token) => this.linkService.PrepareAsync(Text, Schemes, Token));

			if (Link is not null)
				await this.linkService.ExecuteAsync(Link);
		}

		/// <inheritdoc/>
		public byte[] GeneratePng(string Text, int Width, int Height)
		{
			return QrCode.GeneratePng(Text, Width, Height);
		}

		/// <inheritdoc/>
		public byte[] GenerateJpg(string Text, int Width, int Height)
		{
			return QrCode.GenerateJpg(Text, Width, Height);
		}

		/// <summary>
		/// Shows the scanner and waits until it delivers accepted text or closes.
		/// </summary>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <param name="AllowedSchemes">The permitted schemes, or an empty array to accept any text.</param>
		/// <param name="PrepareAsync">Optional preparation that must be ready before the scanner accepts text.</param>
		/// <returns>The accepted text, or <c>null</c> if the scan was cancelled or the camera may not be used.</returns>
		private async Task<string?> ScanCoreAsync(string? Title, string[] AllowedSchemes,
			Func<string, CancellationToken, Task<PreparationStatus>>? PrepareAsync)
		{
			if (!await ServiceRef.PermissionService.CheckCameraPermissionAsync())
				return null;

			ScanQrCodeNavigationArgs Args = new(Title, AllowedSchemes, PrepareAsync);

			// The waiter is taken before navigating, since the scanner may complete and clear it before navigation returns.
			TaskCompletionSource<string?> Completion = Args.QrCodeScanned
				?? throw new InvalidOperationException("The scanner request has no result waiter.");

			try
			{
				await this.navigationService.GoToAsync(nameof(ScanQrCodePage), Args, BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				this.logService.LogException(Ex);
				Completion.TrySetResult(null);
			}

			return await Completion.Task;
		}
	}
}
