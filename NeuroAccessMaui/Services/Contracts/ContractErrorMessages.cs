using System.Globalization;
using System.Reflection;
using NeuroAccessMaui.Resources.Languages;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Turns exceptions from contract actions into one localized message for the user.
	/// </summary>
	/// <remarks>
	/// Server refusals are matched on the stable English texts the Neuron sends, by prefix.
	/// A server that answers in another language falls back to its own text.
	/// </remarks>
	public static class ContractErrorMessages
	{
		private const string NotOpenYetPrefix = "Contract will be open for signatures after ";
		private const string SigningClosedPrefix = "Contract closed for signatures as of ";
		private const string UtcSuffix = "(UTC)";
		private const string MissingPropertyPrefix = "Legal identity does not have required property ";
		private const string MissingPropertySuffix = " defined.";
		private const string InvalidParameterPrefix = "Contract parameter ";
		private const string InvalidParameterInfix = " contains invalid value";

		/// <summary>
		/// Server and client texts that map directly to a resource key, matched by prefix.
		/// </summary>
		private static readonly (string Prefix, string Key)[] fixedTexts =
		[
			("No more signatures of specified role allowed.", nameof(AppResources.ContractErrorRoleFull)),
			("No such role defined in contract.", nameof(AppResources.ContractErrorNoSuchRole)),
			("Legal identity not in explicitly defined list of parts in contract.", nameof(AppResources.ContractErrorNotAPart)),
			("Not a part in the contract.", nameof(AppResources.ContractErrorNotAPart)),
			("No approved legal identity available on this device", nameof(AppResources.ContractErrorNoKeysOnDevice)),
			("Private keys are not available on this device", nameof(AppResources.ContractErrorNoKeysOnDevice)),
			("Only the creator can", nameof(AppResources.ContractErrorCreatorOnly)),
			("Contract cannot be deleted before its required archivation period expires.", nameof(AppResources.ContractErrorArchivePeriod)),
			("Contract is legally binding and cannot be", nameof(AppResources.ContractErrorBinding)),
			("Your role cannot revoke signature/consent.", nameof(AppResources.ContractErrorCannotRevoke)),
			("Contract is a template only, and cannot be signed.", nameof(AppResources.ContractErrorTemplateOnly)),
			("Contract is obsoleted.", nameof(AppResources.ContractErrorObsoleted)),
			("Contract is deleted.", nameof(AppResources.ContractErrorDeleted)),
			("Contract proposal has failed.", nameof(AppResources.ContractErrorFailed)),
			("Contract proposal has been rejected.", nameof(AppResources.ContractErrorRejected)),
			("Contract is not yet approved to be signed.", nameof(AppResources.ContractErrorNotApproved)),
			("Contract not found.", nameof(AppResources.ContractErrorNotFound))
		];

		/// <summary>
		/// Gets a localized message for an exception raised by a contract action.
		/// </summary>
		/// <param name="Error">The exception that was raised.</param>
		/// <returns>A localized message to show to the user.</returns>
		public static string GetMessage(Exception Error)
		{
			return GetMessage(Error, null);
		}

		/// <summary>
		/// Gets a localized message for an exception raised by a contract action.
		/// </summary>
		/// <param name="Error">The exception that was raised.</param>
		/// <param name="Contract">The contract the action was performed on, if known. Its signing window is shown in local time.</param>
		/// <returns>A localized message to show to the user.</returns>
		public static string GetMessage(Exception Error, Contract? Contract)
		{
			Exception Inner = Unwrap(Error);

			if (Inner is TimeoutException || Inner is RemoteServerTimeoutException)
				return ServiceRef.Localizer[nameof(AppResources.AppNotConnected)];

			string Text = Inner.Message?.Trim() ?? string.Empty;

			if (Text.StartsWith(NotOpenYetPrefix, StringComparison.Ordinal))
			{
				string When = FormatWindowTime(Contract?.SignAfter, Text, NotOpenYetPrefix);
				return ServiceRef.Localizer[nameof(AppResources.ContractErrorNotOpenYet), When];
			}

			if (Text.StartsWith(SigningClosedPrefix, StringComparison.Ordinal))
			{
				string When = FormatWindowTime(Contract?.SignBefore, Text, SigningClosedPrefix);
				return ServiceRef.Localizer[nameof(AppResources.ContractErrorSigningClosed), When];
			}

			if (Text.StartsWith(MissingPropertyPrefix, StringComparison.Ordinal))
			{
				string Property = Text[MissingPropertyPrefix.Length..];
				if (Property.EndsWith(MissingPropertySuffix, StringComparison.Ordinal))
					Property = Property[..^MissingPropertySuffix.Length];

				return ServiceRef.Localizer[nameof(AppResources.ContractErrorMissingProperty), Property.Trim()];
			}

			if (Text.StartsWith(InvalidParameterPrefix, StringComparison.Ordinal))
			{
				int Index = Text.IndexOf(InvalidParameterInfix, InvalidParameterPrefix.Length, StringComparison.Ordinal);
				if (Index > InvalidParameterPrefix.Length)
				{
					string Name = Text[InvalidParameterPrefix.Length..Index];
					return ServiceRef.Localizer[nameof(AppResources.ContractErrorInvalidParameter), Name];
				}
			}

			foreach ((string Prefix, string Key) in fixedTexts)
			{
				if (Text.StartsWith(Prefix, StringComparison.Ordinal))
					return ServiceRef.Localizer[Key];
			}

			if (Inner is ItemNotFoundException)
				return ServiceRef.Localizer[nameof(AppResources.ContractErrorNotFound)];

			if (Inner is not StanzaExceptionException && !ServiceRef.NetworkService.IsOnline)
				return ServiceRef.Localizer[nameof(AppResources.AppNotConnected)];

			if (!string.IsNullOrWhiteSpace(Text))
				return Text;

			return ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)];
		}

		/// <summary>
		/// Gets the exception that carries the actual error, skipping wrappers added by tasks and reflection.
		/// </summary>
		/// <param name="Error">The exception that was raised.</param>
		/// <returns>The innermost relevant exception.</returns>
		private static Exception Unwrap(Exception Error)
		{
			Exception Result = Error;

			while (true)
			{
				if (Result is AggregateException Aggregate && Aggregate.InnerExceptions.Count > 0)
					Result = Aggregate.InnerExceptions[0];
				else if (Result is TargetInvocationException Invocation && Invocation.InnerException is not null)
					Result = Invocation.InnerException;
				else
					return Result;
			}
		}

		/// <summary>
		/// Formats a signing window time for the user. Uses the contract's own time in local time when known,
		/// and otherwise the UTC time from the server's text.
		/// </summary>
		/// <param name="Time">The time from the contract, if known.</param>
		/// <param name="Text">The server's error text.</param>
		/// <param name="Prefix">The prefix that precedes the time in the server's text.</param>
		/// <returns>The time as text.</returns>
		private static string FormatWindowTime(DateTime? Time, string Text, string Prefix)
		{
			if (Time.HasValue)
				return Time.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

			string ServerTime = Text[Prefix.Length..];
			if (ServerTime.EndsWith(UtcSuffix, StringComparison.Ordinal))
				ServerTime = ServerTime[..^UtcSuffix.Length].Trim() + " UTC";

			return ServerTime.Trim();
		}
	}
}
