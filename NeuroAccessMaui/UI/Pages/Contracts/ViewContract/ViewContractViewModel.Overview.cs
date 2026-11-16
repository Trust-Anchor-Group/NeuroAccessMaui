using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// What the contract page and the sign sheet show first: where the contract stands for the user, a short summary, the
	/// key values, and the one thing to do next.
	/// </summary>
	public partial class ViewContractViewModel
	{
		/// <summary>
		/// How many values the page shows before "All details".
		/// </summary>
		private const int keyDetailCount = 3;

		/// <summary>
		/// How close the signing deadline is before it is shown as urgent.
		/// </summary>
		private static readonly TimeSpan nearDeadline = TimeSpan.FromDays(3);

		/// <summary>
		/// The properties whose changes can change where the contract stands for the user.
		/// </summary>
		private static readonly HashSet<string> statusSources =
		[
			nameof(CanSign), nameof(Contract), nameof(IsDeletedOnServer), nameof(CanInviteParts),
			nameof(IsAwaitingPostCreateCompletion), nameof(SelectedRole), nameof(ProposalRole), nameof(ProposalFriendlyName)
		];

		/// <summary>
		/// If the user created the contract, as far as is known.
		/// </summary>
		private bool isCreator;

		/// <summary>
		/// The first sentences of the contract's text that have all their values, as a summary.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSummary))]
		private string summaryText = string.Empty;

		/// <summary>
		/// The contract's key values: amounts first, then the others in the template's order.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasKeyDetails))]
		private IReadOnlyList<ObservableParameter> keyDetails = [];

		/// <summary>
		/// The link to every value, such as "All 9 details".
		/// </summary>
		[ObservableProperty]
		private string allDetailsText = string.Empty;

		/// <summary>
		/// If the contract has more values than the key ones.
		/// </summary>
		[ObservableProperty]
		private bool hasMoreDetails;

		/// <summary>
		/// What signing in the selected role takes from the user's ID.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasFromIdValues))]
		private IReadOnlyList<ContractIdValue> fromIdValues = [];

		/// <summary>
		/// If the contract has a summary.
		/// </summary>
		public bool HasSummary => this.SummaryText.Length > 0;

		/// <summary>
		/// If the contract has key values to show.
		/// </summary>
		public bool HasKeyDetails => this.KeyDetails.Count > 0;

		/// <summary>
		/// If signing takes anything from the user's ID.
		/// </summary>
		public bool HasFromIdValues => this.FromIdValues.Count > 0;

		/// <summary>
		/// If the contract page itself is shown, which has the title in its header card instead of the top bar.
		/// </summary>
		public bool IsOverviewShown => this.CurrentState == nameof(ViewContractStep.Overview);

		/// <summary>
		/// If the contract still collects signatures.
		/// </summary>
		private bool IsOpenForSigning => !this.IsDeletedOnServer &&
			this.Contract?.ContractState is ContractState.Proposed or ContractState.Approved or ContractState.BeingSigned;

		/// <summary>
		/// If the user has done their part, as signer or creator, and others still have to sign.
		/// </summary>
		public bool IsWaitingForOthers => this.IsOpenForSigning && !this.CanSign && (this.AlreadySigned || this.isCreator);

		/// <summary>
		/// If the user can only read the contract, since they have no role in it.
		/// </summary>
		private bool HasNoRole => this.IsOpenForSigning && !this.CanSign && !this.IsWaitingForOthers &&
			this.Contract?.Roles.Any(Role => Role.IsMyRole) != true;

		/// <summary>
		/// Where the contract stands for the user, such as "Waiting for your signature".
		/// </summary>
		public string StatusText
		{
			get
			{
				if (this.CanSign)
					return ServiceRef.Localizer[nameof(AppResources.ContractStatusNeedsYou)];

				if (this.IsWaitingForOthers)
					return ServiceRef.Localizer[nameof(AppResources.ContractStatusWaitingForOthers)];

				if (this.HasNoRole)
					return ServiceRef.Localizer[nameof(AppResources.ContractStatusForReading)];

				return this.StateText;
			}
		}

		/// <summary>
		/// If the status asks something of the user.
		/// </summary>
		public bool IsStateWarning => this.CanSign;

		/// <summary>
		/// One sentence on the status, such as who is still to sign, or when everyone signed.
		/// </summary>
		public string StatusDetailText
		{
			get
			{
				Contract? Current = this.Contract?.Contract;
				if (Current is null || this.CanSign)
					return string.Empty;

				if (this.IsWaitingForOthers && this.AlreadySigned)
					return this.GetWaitingText();

				if (this.HasNoRole)
					return ServiceRef.Localizer[nameof(AppResources.ContractNoRoleForYou)];

				if (this.IsDeletedOnServer)
					return string.Empty;

				if (Current.State == ContractState.Signed)
				{
					DateTime SignedAt = Current.ClientSignatures?.Length > 0
						? Current.ClientSignatures.Max(Signature => Signature.Timestamp)
						: Current.Updated;

					string Text = ServiceRef.Localizer[nameof(AppResources.ContractSignedByAllOn), FormatDayWithYear(SignedAt)];

					if (this.HasValidTo)
						Text += " " + ServiceRef.Localizer[nameof(AppResources.ContractValidUntilSentence), FormatDayWithYear(Current.To)];

					return Text;
				}

				if (Current.State is ContractState.Rejected or ContractState.Failed)
					return ServiceRef.Localizer[nameof(AppResources.ContractNobodyCanSign)];

				return string.Empty;
			}
		}

		/// <summary>
		/// If there is a sentence on the status.
		/// </summary>
		public bool HasStatusDetail => this.StatusDetailText.Length > 0;

		/// <summary>
		/// The signatures made, counted against the ones needed.
		/// </summary>
		private int SignatureCount => this.Contract?.Contract.ClientSignatures?.Length ?? 0;

		/// <summary>
		/// The signatures the contract needs, at least, to be signed.
		/// </summary>
		private int RequiredSignatureCount => this.Contract?.Roles.Sum(Role => Role.MinCount) ?? 0;

		/// <summary>
		/// If the signing progress is shown.
		/// </summary>
		public bool HasProgress => this.IsOpenForSigning && this.RequiredSignatureCount > 0;

		/// <summary>
		/// How far signing has come, from 0 to 1.
		/// </summary>
		public double ProgressValue => this.RequiredSignatureCount == 0
			? 0
			: Math.Min(1.0, (double)this.SignatureCount / this.RequiredSignatureCount);

		/// <summary>
		/// The signing progress, such as "1 of 2 signed".
		/// </summary>
		public string ProgressText => ServiceRef.Localizer[nameof(AppResources.ContractProgress),
			Math.Min(this.SignatureCount, this.RequiredSignatureCount), this.RequiredSignatureCount];

		/// <summary>
		/// If the contract must be signed before a set time.
		/// </summary>
		public bool HasDeadline => this.IsOpenForSigning && this.Contract?.Contract.SignBefore.HasValue == true;

		/// <summary>
		/// The signing deadline and the time left, such as "Sign by 30 November · 3 days left".
		/// </summary>
		public string DeadlineText
		{
			get
			{
				if (!this.HasDeadline)
					return string.Empty;

				DateTime Before = this.Contract!.Contract.SignBefore!.Value;
				TimeSpan Left = ToUtc(Before) - DateTime.UtcNow;
				string Text = ServiceRef.Localizer[nameof(AppResources.ContractSignBy), FormatDay(Before)];

				if (Left.TotalDays >= 2)
					return Text + " · " + ServiceRef.Localizer[nameof(AppResources.ContractDaysLeft), (int)Math.Floor(Left.TotalDays)];

				if (Left.TotalDays >= 1)
					return Text + " · " + ServiceRef.Localizer[nameof(AppResources.ContractOneDayLeft)];

				if (Left > TimeSpan.Zero)
					return Text + " · " + ServiceRef.Localizer[nameof(AppResources.ContractLessThanDayLeft)];

				return Text;
			}
		}

		/// <summary>
		/// If the signing deadline is close.
		/// </summary>
		public bool IsDeadlineNear => this.HasDeadline && ToUtc(this.Contract!.Contract.SignBefore!.Value) - DateTime.UtcNow <= nearDeadline;

		/// <summary>
		/// Who invited the user to sign, such as "Anna Berg invited you to sign".
		/// </summary>
		public string InvitedByText => this.HasProposalFriendlyName
			? ServiceRef.Localizer[nameof(AppResources.ContractInvitedYou), this.ProposalFriendlyName ?? string.Empty]
			: string.Empty;

		/// <summary>
		/// If the page says who invited the user, or shows their message.
		/// </summary>
		public bool HasInvitation => this.CanSign && (this.HasProposalFriendlyName || this.HasProposalMessage);

		/// <summary>
		/// The role the user is about to sign as, when there is only one.
		/// </summary>
		private string? SingleSigningRole => !string.IsNullOrEmpty(this.ProposalRole)
			? this.ProposalRole
			: this.SignableRoles.Count == 1 ? this.SignableRoles[0].Name : null;

		/// <summary>
		/// The main button of the page, such as "Review and sign as Buyer".
		/// </summary>
		public string PrimaryActionText => this.SingleSigningRole is string Role
			? ServiceRef.Localizer[nameof(AppResources.ContractReviewAndSignAs), Role]
			: ServiceRef.Localizer[nameof(AppResources.ContractReviewAndSign)];

		/// <summary>
		/// If the page offers to invite the people who haven't signed, when the user has nothing to sign.
		/// </summary>
		public bool CanShowInviteBar => !this.IsAwaitingPostCreateCompletion && !this.CanSign && this.CanInviteParts;

		/// <summary>
		/// The heading of the sign sheet, such as "Sign as Buyer".
		/// </summary>
		public string SignTitle => this.SelectedRole is null
			? ServiceRef.Localizer[nameof(AppResources.Sign)]
			: ServiceRef.Localizer[nameof(AppResources.ContractSignAs), this.SelectedRole.Name];

		/// <summary>
		/// If the user chooses between roles on the sign sheet.
		/// </summary>
		public bool HasRoleChoice => this.SignableRoles.Count > 1;

		/// <summary>
		/// Who can see the contract once signed, and that a full set of signatures can't be undone.
		/// </summary>
		public string SignVisibilityText
		{
			get
			{
				string Who = this.Contract?.Visibility switch
				{
					ContractVisibility.Public or ContractVisibility.PublicSearchable => ServiceRef.Localizer[nameof(AppResources.ContractSeenByAnyone)],
					ContractVisibility.DomainAndParts => ServiceRef.Localizer[nameof(AppResources.ContractSeenByDomain)],
					_ => ServiceRef.Localizer[nameof(AppResources.ContractSeenByParts)]
				};

				return Who + " " + ServiceRef.Localizer[nameof(AppResources.ContractCantUndo)];
			}
		}

		/// <summary>
		/// If anyone can see the contract, which the sign sheet points out.
		/// </summary>
		public bool IsVisibleToAnyone => this.Contract?.Visibility is ContractVisibility.Public or ContractVisibility.PublicSearchable;

		/// <summary>
		/// Chooses the role to sign as, on the sign sheet.
		/// </summary>
		/// <param name="Role">The role.</param>
		[RelayCommand]
		private void SelectRole(ObservableRole? Role)
		{
			if (Role is not null)
				this.SelectedRole = Role;
		}

		/// <summary>
		/// Ticks or unticks "I have read the contract", for taps on the text next to the box.
		/// </summary>
		[RelayCommand]
		private void ToggleContractOk()
		{
			this.IsContractOk = !this.IsContractOk;
		}

		/// <summary>
		/// Updates what depends on a property that changed.
		/// </summary>
		/// <param name="PropertyName">The property.</param>
		private void OnPageValueChanged(string? PropertyName)
		{
			if (PropertyName is null || !statusSources.Contains(PropertyName))
				return;

			this.OnPageStatusChanged();

			if (PropertyName is nameof(this.SelectedRole) or nameof(this.Contract))
				this.UpdateFromIdValues();
		}

		/// <summary>
		/// Notifies everything that shows where the contract stands for the user.
		/// </summary>
		private void OnPageStatusChanged()
		{
			this.OnPropertyChanged(nameof(this.IsWaitingForOthers));
			this.OnPropertyChanged(nameof(this.StatusText));
			this.OnPropertyChanged(nameof(this.IsStateWarning));
			this.OnPropertyChanged(nameof(this.StatusDetailText));
			this.OnPropertyChanged(nameof(this.HasStatusDetail));
			this.OnPropertyChanged(nameof(this.HasProgress));
			this.OnPropertyChanged(nameof(this.ProgressValue));
			this.OnPropertyChanged(nameof(this.ProgressText));
			this.OnPropertyChanged(nameof(this.HasDeadline));
			this.OnPropertyChanged(nameof(this.DeadlineText));
			this.OnPropertyChanged(nameof(this.IsDeadlineNear));
			this.OnPropertyChanged(nameof(this.InvitedByText));
			this.OnPropertyChanged(nameof(this.HasInvitation));
			this.OnPropertyChanged(nameof(this.PrimaryActionText));
			this.OnPropertyChanged(nameof(this.CanShowInviteBar));
			this.OnPropertyChanged(nameof(this.SignTitle));
			this.OnPropertyChanged(nameof(this.HasRoleChoice));
			this.OnPropertyChanged(nameof(this.SignVisibilityText));
			this.OnPropertyChanged(nameof(this.IsVisibleToAnyone));
		}

		/// <summary>
		/// Follows the roles, so names and signatures that arrive later show in the status.
		/// </summary>
		private void Contract_RoleChanged(object? Sender, PropertyChangedEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(this.OnPageStatusChanged);
		}

		/// <summary>
		/// Picks the summary and the key values from the contract's values.
		/// </summary>
		/// <param name="Displayed">Every value the details page shows, in the template's order.</param>
		private void PrepareDigest(IReadOnlyList<ObservableParameter> Displayed)
		{
			if (this.Contract is null)
			{
				this.SummaryText = string.Empty;
				this.KeyDetails = [];
				this.HasMoreDetails = false;
				return;
			}

			try
			{
				this.SummaryText = ContractDigest.GetSummary(this.Contract.Contract, this.Contract.Parameters);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.SummaryText = string.Empty;
			}

			this.KeyDetails = ContractDigest.GetKeyDetails(this.Contract.Contract, Displayed, keyDetailCount);
			this.HasMoreDetails = Displayed.Count > this.KeyDetails.Count;
			this.AllDetailsText = ServiceRef.Localizer[nameof(AppResources.ContractAllDetails), Displayed.Count];
		}

		/// <summary>
		/// Lists what signing in the selected role takes from the user's ID: the values for the next signature of the role.
		/// </summary>
		private void UpdateFromIdValues()
		{
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			Contract? Current = this.Contract?.Contract;
			string? RoleName = this.SelectedRole?.Name;

			if (Identity is null || Current is null || RoleName is null || this.Contract is null)
			{
				this.FromIdValues = [];
				return;
			}

			int NextIndex = (Current.ClientSignatures?.Count(Signature => Signature.Role == RoleName) ?? 0) + 1;
			List<ContractIdValue> Values = [];

			foreach (ObservableParameter Parameter in this.Contract.Parameters)
			{
				if (Parameter.Parameter is not RoleParameter RoleParameter ||
					RoleParameter.Role != RoleName ||
					RoleParameter.Index != NextIndex)
				{
					continue;
				}

				string? Value = ContractSigningGuard.GetShownValue(Identity, RoleParameter);

				if (Value is null)
					Values.Add(new ContractIdValue(Parameter.Label, ServiceRef.Localizer[nameof(AppResources.ContractNotInYourId)], true));
				else if (Value.Length > 0)
					Values.Add(new ContractIdValue(Parameter.Label, Value, false));
			}

			this.FromIdValues = Values;
		}

		/// <summary>
		/// Says who still has to sign, after the user signed: by name where the contract names them, else how many.
		/// </summary>
		/// <returns>The sentence.</returns>
		private string GetWaitingText()
		{
			List<string> Names = [];
			int Missing = 0;

			foreach (ObservableRole Role in this.Contract?.Roles ?? [])
			{
				int Signed = Role.SignatureCount;
				if (Signed < Role.MinCount)
					Missing += Role.MinCount - Signed;

				foreach (ObservablePart Part in Role.Parts)
				{
					if (!Part.HasSigned && !Part.IsMe && !string.IsNullOrEmpty(Part.FriendlyName))
						Names.Add(Part.FriendlyName + " (" + Role.Name + ")");
				}
			}

			if (Names.Count > 0 && Names.Count >= Missing)
				return ServiceRef.Localizer[nameof(AppResources.ContractWaitingForNames), string.Join(", ", Names)];

			if (Missing > 0)
				return ServiceRef.Localizer[nameof(AppResources.ContractWaitingForCount), Missing];

			return string.Empty;
		}

		/// <summary>
		/// Treats a contract time without a kind as UTC, as the server sends them.
		/// </summary>
		private static DateTime ToUtc(DateTime Value)
		{
			return Value.Kind switch
			{
				DateTimeKind.Utc => Value,
				DateTimeKind.Local => Value.ToUniversalTime(),
				_ => DateTime.SpecifyKind(Value, DateTimeKind.Utc)
			};
		}

		/// <summary>
		/// Formats the local day and month of a contract time, such as "30 November".
		/// </summary>
		private static string FormatDay(DateTime Value)
		{
			return ToUtc(Value).ToLocalTime().ToString("M", CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Formats the local date of a contract time, such as "2 Mar 2026".
		/// </summary>
		private static string FormatDayWithYear(DateTime Value)
		{
			return ToUtc(Value).ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture);
		}
	}
}
