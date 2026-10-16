using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Decides which actions to offer on a contract, following the rules the Neuron applies.
	/// </summary>
	/// <remarks>
	/// These rules only avoid offering actions the server would refuse. The server stays the authority,
	/// and its refusals are still shown through <see cref="ContractErrorMessages"/>.
	/// </remarks>
	public static class ContractActionRules
	{
		/// <summary>
		/// Checks if the user can delete a contract.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="IsCreator">If the user's account created the contract.</param>
		/// <param name="IsBindingIgnoringTime">If the contract is legally binding when the current time is not checked.</param>
		/// <param name="UtcNow">The current time, in UTC.</param>
		/// <returns>If Delete should be offered.</returns>
		public static bool CanDelete(Contract Contract, bool IsCreator, bool IsBindingIgnoringTime, DateTime UtcNow)
		{
			if (!IsCreator || Contract.State == ContractState.Deleted)
				return false;

			if (!IsBindingIgnoringTime)
				return true;

			try
			{
				DateTime ArchivedUntil = Contract.To.ToUniversalTime();
				if (Contract.ArchiveRequired is Duration ArchiveRequired)
					ArchivedUntil += ArchiveRequired;

				return UtcNow >= ArchivedUntil;
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
		}

		/// <summary>
		/// Checks if the user can withdraw (obsolete) a contract. Withdrawing cancels the contract for everyone.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="IsCreator">If the user's account created the contract.</param>
		/// <param name="IsBindingNow">If the contract is legally binding at the current time.</param>
		/// <param name="LegalId">The user's legal identity ID, if any.</param>
		/// <param name="BareJid">The user's bare JID, if known.</param>
		/// <returns>If Withdraw should be offered.</returns>
		public static bool CanWithdraw(Contract Contract, bool IsCreator, bool IsBindingNow, string? LegalId, string? BareJid)
		{
			if (Contract.State is not (ContractState.Proposed or ContractState.Approved or ContractState.BeingSigned or ContractState.Signed))
				return false;

			if (IsCreator && !IsBindingNow)
				return true;

			if (Contract.ClientSignatures is null || Contract.Roles is null)
				return false;

			foreach (ClientSignature Signature in Contract.ClientSignatures)
			{
				bool IsMine =
					(!string.IsNullOrEmpty(LegalId) && string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) ||
					(!string.IsNullOrEmpty(BareJid) && string.Equals(Signature.BareJid, BareJid, StringComparison.OrdinalIgnoreCase));

				if (!IsMine)
					continue;

				foreach (Role Role in Contract.Roles)
				{
					if (Role.CanRevoke && string.Equals(Role.Name, Signature.Role, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the user can sign a contract: the contract is open for signatures, inside its signing window,
		/// not a template, and has a role the user can still sign in.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="LegalId">The user's legal identity ID, if any.</param>
		/// <param name="UtcNow">The current time, in UTC.</param>
		/// <returns>If signing should be offered.</returns>
		public static bool CanSign(Contract Contract, string? LegalId, DateTime UtcNow)
		{
			if (string.IsNullOrEmpty(LegalId))
				return false;

			if (Contract.State is not (ContractState.Approved or ContractState.BeingSigned or ContractState.Signed))
				return false;

			if (Contract.PartsMode == ContractParts.TemplateOnly)
				return false;

			if (Contract.SignAfter.HasValue && UtcNow < Contract.SignAfter.Value.ToUniversalTime())
				return false;

			if (Contract.SignBefore.HasValue && UtcNow > Contract.SignBefore.Value.ToUniversalTime())
				return false;

			if (Contract.Roles is null)
				return false;

			foreach (Role Role in Contract.Roles)
			{
				if (CanSignInRole(Contract, Role, LegalId))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if a part's signature can be viewed.
		/// </summary>
		/// <param name="Signature">The part's client signature, if the part has signed.</param>
		/// <returns>If "View signature" should be offered.</returns>
		public static bool CanViewSignature(ClientSignature? Signature)
		{
			return Signature is not null;
		}

		/// <summary>
		/// Checks if the user can still sign a contract in a given role: the role isn't full, the user hasn't
		/// signed in it, and, for named parts, the user is listed for it.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="Role">The role.</param>
		/// <param name="LegalId">The user's legal identity ID.</param>
		/// <returns>If the user can sign in the role.</returns>
		private static bool CanSignInRole(Contract Contract, Role Role, string LegalId)
		{
			int Count = 0;

			if (Contract.ClientSignatures is not null)
			{
				foreach (ClientSignature Signature in Contract.ClientSignatures)
				{
					if (!string.Equals(Signature.Role, Role.Name, StringComparison.OrdinalIgnoreCase))
						continue;

					if (string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase))
						return false;

					Count++;
				}
			}

			if (Count >= Role.MaxCount)
				return false;

			if (Contract.PartsMode != ContractParts.ExplicitlyDefined)
				return true;

			if (Contract.Parts is null)
				return false;

			foreach (Part Part in Contract.Parts)
			{
				if (string.Equals(Part.Role, Role.Name, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(Part.LegalId, LegalId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
