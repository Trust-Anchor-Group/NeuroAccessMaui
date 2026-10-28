using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Security;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Checks, before signing, what the Neuron would refuse, so the user is told before they start.
	/// </summary>
	/// <remarks>
	/// The server stays the authority. Its refusals are still shown through <see cref="ContractErrorMessages"/>.
	/// </remarks>
	public static class ContractSigningGuard
	{
		/// <summary>
		/// How much the device clock may differ from the server's at the edges of the signing window.
		/// </summary>
		private static readonly TimeSpan clockTolerance = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Checks if the user can sign a contract.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="RoleName">The role to sign as. When null, the role checks pass if any role can be signed.</param>
		/// <returns>Every blocker, in the order the server checks them. Empty if nothing is known to stop the signature.</returns>
		public static async Task<IReadOnlyList<ContractSigningBlocker>> CheckAsync(Contract Contract, string? RoleName)
		{
			List<ContractSigningBlocker> Blockers = [];
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;

			// 1. An approved identity, with its keys on this device.
			if (Identity is null || Identity.State != IdentityState.Approved)
			{
				Blockers.Add(new ContractSigningBlocker(
					ServiceRef.Localizer[nameof(AppResources.ContractGuardIdentityNotApproved)],
					ServiceRef.Localizer[nameof(AppResources.ContractGuardOpenIdentity)],
					OpenIdentityAsync));
			}
			else if (!await HasKeysAsync(Identity))
				Blockers.Add(new ContractSigningBlocker(ServiceRef.Localizer[nameof(AppResources.ContractErrorNoKeysOnDevice)]));

			// 2. The signing window, compared in UTC and shown in local time.
			DateTime UtcNow = DateTime.UtcNow;

			if (Contract.SignAfter is DateTime SignAfter && UtcNow + clockTolerance < SignAfter.ToUniversalTime())
			{
				Blockers.Add(new ContractSigningBlocker(
					ServiceRef.Localizer[nameof(AppResources.ContractErrorNotOpenYet), FormatTime(SignAfter)]));
			}

			if (Contract.SignBefore is DateTime SignBefore && UtcNow - clockTolerance > SignBefore.ToUniversalTime())
			{
				Blockers.Add(new ContractSigningBlocker(
					ServiceRef.Localizer[nameof(AppResources.ContractErrorSigningClosed), FormatTime(SignBefore)]));
			}

			if (Identity is null)
				return Blockers;

			// 3 to 6. The role.
			if (!string.IsNullOrEmpty(RoleName))
			{
				Blockers.AddRange(await CheckRoleAsync(Contract, RoleName, Identity));
				return Blockers;
			}

			List<ContractSigningBlocker> RoleBlockers = [];

			foreach (Role Role in Contract.Roles ?? [])
			{
				List<ContractSigningBlocker> ForRole = await CheckRoleAsync(Contract, Role.Name, Identity);
				if (ForRole.Count == 0)
					return Blockers;

				foreach (ContractSigningBlocker Blocker in ForRole)
				{
					if (!RoleBlockers.Any(Existing => Existing.Message == Blocker.Message))
						RoleBlockers.Add(Blocker);
				}
			}

			Blockers.AddRange(RoleBlockers);
			return Blockers;
		}

		/// <summary>
		/// Runs the checks for one role: room left, named parts, required properties, and an earlier signature.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="RoleName">The role.</param>
		/// <param name="Identity">The user's identity.</param>
		/// <returns>The blockers for the role.</returns>
		private static async Task<List<ContractSigningBlocker>> CheckRoleAsync(Contract Contract, string RoleName, LegalIdentity Identity)
		{
			List<ContractSigningBlocker> Blockers = [];
			Role? Role = Contract.Roles?.FirstOrDefault(Item => Item.Name == RoleName);

			if (Role is null)
			{
				Blockers.Add(new ContractSigningBlocker(ServiceRef.Localizer[nameof(AppResources.ContractErrorNoSuchRole)]));
				return Blockers;
			}

			ClientSignature[] Signatures = Contract.ClientSignatures ?? [];
			int RoleSignatureCount = Signatures.Count(Signature => Signature.Role == RoleName);
			bool AlreadySigned = Signatures.Any(Signature => Signature.Role == RoleName && IsMine(Signature, Identity));

			if (AlreadySigned)
			{
				// 6. Signing again would change nothing, or add a second signature by the same person.
				Blockers.Add(new ContractSigningBlocker(ServiceRef.Localizer[nameof(AppResources.ContractGuardAlreadySigned)]));
				return Blockers;
			}

			// 3. The role has room for another signature.
			if (RoleSignatureCount >= Role.MaxCount)
				Blockers.Add(new ContractSigningBlocker(ServiceRef.Localizer[nameof(AppResources.ContractErrorRoleFull)]));

			// 4. Named parts.
			if (Contract.PartsMode == ContractParts.TemplateOnly)
				Blockers.Add(new ContractSigningBlocker(ServiceRef.Localizer[nameof(AppResources.ContractErrorTemplateOnly)]));
			else if (Contract.PartsMode == ContractParts.ExplicitlyDefined && !await IsNamedPartAsync(Contract, RoleName, Identity.Id, Signatures, true))
				Blockers.Add(new ContractSigningBlocker(ServiceRef.Localizer[nameof(AppResources.ContractErrorNotAPart)]));

			// 5. Required properties, for the index the next signature of the role gets.
			int NextIndex = RoleSignatureCount + 1;

			foreach (Parameter Parameter in Contract.Parameters ?? [])
			{
				if (Parameter is RoleParameter RoleParameter &&
					RoleParameter.Required &&
					RoleParameter.Role == RoleName &&
					RoleParameter.Index == NextIndex &&
					!HasProperty(Identity, RoleParameter))
				{
					Blockers.Add(new ContractSigningBlocker(
						ServiceRef.Localizer[nameof(AppResources.ContractErrorMissingProperty), RoleParameter.Property]));
				}
			}

			return Blockers;
		}

		/// <summary>
		/// Gets the roles of a named-parts contract that the user isn't listed for, but can sign for an earlier identity
		/// of theirs, because their identity was renewed.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="LegalId">The user's current legal identity ID.</param>
		/// <returns>The names of those roles. Empty when nothing can be checked.</returns>
		public static async Task<IReadOnlyCollection<string>> GetRolesSignableAsEarlierIdentityAsync(Contract Contract, string LegalId)
		{
			HashSet<string> Result = new(StringComparer.OrdinalIgnoreCase);

			if (Contract.PartsMode != ContractParts.ExplicitlyDefined || Contract.Roles is null)
				return Result;

			ClientSignature[] Signatures = Contract.ClientSignatures ?? [];

			foreach (Role Role in Contract.Roles)
			{
				bool Listed = Contract.Parts?.Any(Part => Part.Role == Role.Name &&
					string.Equals(Part.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) == true;

				if (!Listed && await IsNamedPartAsync(Contract, Role.Name, LegalId, Signatures, false))
					Result.Add(Role.Name);
			}

			return Result;
		}

		/// <summary>
		/// Checks if the user is named for a role, directly or through an earlier identity of the same person.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="RoleName">The role.</param>
		/// <param name="LegalId">The user's legal identity ID.</param>
		/// <param name="Signatures">The contract's client signatures.</param>
		/// <param name="TrueIfUnknown">What to answer when the server can't be asked.</param>
		/// <returns>If the user may sign as one of the named parts.</returns>
		private static async Task<bool> IsNamedPartAsync(Contract Contract, string RoleName, string LegalId, ClientSignature[] Signatures,
			bool TrueIfUnknown)
		{
			List<string> Unsigned = [];

			foreach (Part Part in Contract.Parts ?? [])
			{
				if (Part.Role != RoleName)
					continue;

				if (string.Equals(Part.LegalId, LegalId, StringComparison.OrdinalIgnoreCase))
					return true;

				bool HasSigned = Signatures.Any(Signature => Signature.Role == RoleName &&
					string.Equals(Signature.LegalId, Part.LegalId, StringComparison.OrdinalIgnoreCase));

				if (!HasSigned)
					Unsigned.Add(Part.LegalId);
			}

			foreach (string PartId in Unsigned)
			{
				try
				{
					// A renewed identity can sign for the person's earlier one.
					if (await ServiceRef.XmppService.ContractsClient.CanSignAs(PartId, LegalId))
						return true;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					return TrueIfUnknown;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the user's identity has a property or attachment that a role parameter requires.
		/// Covers the server's pseudo-properties, which it derives from other properties or from the signature.
		/// </summary>
		/// <param name="Identity">The user's identity.</param>
		/// <param name="Parameter">The role parameter.</param>
		/// <returns>If the server will find a value.</returns>
		private static bool HasProperty(LegalIdentity Identity, RoleParameter Parameter)
		{
			string Name = Parameter.Property ?? string.Empty;

			if (Parameter.RefersToAttachment)
			{
				return Identity.Attachments?.Any(Attachment =>
					string.Equals(Attachment.FileName, Name, StringComparison.OrdinalIgnoreCase)) ?? false;
			}

			if (!string.IsNullOrEmpty(GetProperty(Identity, Name)))
				return true;

			switch (Name.ToLowerInvariant())
			{
				case "first":
				case "middle":
				case "last":
					string FullName = GetProperty(Identity, PersonalInformation.FullNameTag);
					if (string.IsNullOrEmpty(FullName))
						return false;

					LegalIdentity.SeparateNames(FullName, out CaseInsensitiveString First, out CaseInsensitiveString Middle,
						out CaseInsensitiveString Last);

					CaseInsensitiveString NamePart = Name.ToLowerInvariant() switch
					{
						"first" => First,
						"middle" => Middle,
						_ => Last
					};

					return !CaseInsensitiveString.IsNullOrEmpty(NamePart);

				case "fullname":
					return HasAny(Identity, PersonalInformation.FirstNameTag, PersonalInformation.MiddleNamesTag, PersonalInformation.LastNamesTag);

				case "fulladdr":
					return HasAny(Identity, PersonalInformation.AddressTag, PersonalInformation.Address2Tag);

				case "fullorgaddr":
					return HasAny(Identity, PersonalInformation.OrganizationAddressTag, PersonalInformation.OrganizationAddress2Tag);

				case "signature":
				case "signaturedate":
				case "signaturetime":
				case "signaturedatetime":
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Checks if an identity has a value for any of the properties.
		/// </summary>
		/// <param name="Identity">The identity.</param>
		/// <param name="Names">The property names.</param>
		/// <returns>If any property has a value.</returns>
		private static bool HasAny(LegalIdentity Identity, params string[] Names)
		{
			foreach (string Name in Names)
			{
				if (!string.IsNullOrEmpty(GetProperty(Identity, Name)))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Gets a property of an identity, ignoring case, including the values the library derives.
		/// </summary>
		/// <param name="Identity">The identity.</param>
		/// <param name="Name">The property name.</param>
		/// <returns>The value, or an empty string.</returns>
		private static string GetProperty(LegalIdentity Identity, string Name)
		{
			if (string.IsNullOrEmpty(Name))
				return string.Empty;

			foreach (Property Property in Identity.Properties ?? [])
			{
				if (string.Equals(Property.Name, Name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(Property.Value))
					return Property.Value;
			}

			return Identity[Name.ToUpperInvariant()] ?? string.Empty;
		}

		/// <summary>
		/// Checks if a signature was made by the user, with this identity or an earlier one of the same account.
		/// </summary>
		/// <param name="Signature">The signature.</param>
		/// <param name="Identity">The user's identity.</param>
		/// <returns>If the signature is the user's.</returns>
		private static bool IsMine(ClientSignature Signature, LegalIdentity Identity)
		{
			if (string.Equals(Signature.LegalId, Identity.Id, StringComparison.OrdinalIgnoreCase))
				return true;

			string BareJid = ServiceRef.XmppService.BareJid;
			return !string.IsNullOrEmpty(BareJid) && string.Equals(Signature.BareJid, BareJid, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks if this device holds a key that can sign for the identity.
		/// </summary>
		/// <param name="Identity">The identity.</param>
		/// <returns>If the keys are available, or true if it can't be checked.</returns>
		private static async Task<bool> HasKeysAsync(LegalIdentity Identity)
		{
			try
			{
				return await ServiceRef.XmppService.HasUsableKeyAsync(Identity);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return true;
			}
		}

		/// <summary>
		/// Formats a signing window time in local time.
		/// </summary>
		/// <param name="Time">The time.</param>
		/// <returns>The time as text.</returns>
		private static string FormatTime(DateTime Time)
		{
			return Time.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Opens the user's identity, after they confirm with PIN or biometric.
		/// </summary>
		private static async Task OpenIdentityAsync()
		{
			if (await ServiceRef.AuthenticationService.AuthenticateUserAsync(AuthenticationPurpose.ViewId))
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage));
		}
	}
}
