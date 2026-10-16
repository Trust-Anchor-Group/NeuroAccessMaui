using System.Reflection;
using NeuroAccessMaui.Exceptions;
using NeuroAccessMaui.Resources.Languages;
using Waher.Networking.XMPP.StanzaErrors;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Turns exceptions from token actions into localized messages for the user.
	/// </summary>
	public static class TokenErrors
	{
		/// <summary>
		/// Gets a localized message for an exception raised by a token action, when the exception has a known meaning.
		/// </summary>
		/// <param name="Error">The exception that was raised.</param>
		/// <returns>A localized message, or null when there is no specific message for the exception.</returns>
		public static string? ToMessage(Exception Error)
		{
			Exception Inner = Unwrap(Error);

			if (Inner is ForbiddenException || Inner is NotAllowedException)
				return ServiceRef.Localizer[nameof(AppResources.NotAllowedForToken)];

			if (Inner is ItemNotFoundException)
				return ServiceRef.Localizer[nameof(AppResources.TokenNotFound)];

			if (IsUnreachable(Inner))
				return ServiceRef.Localizer[nameof(AppResources.TokenNeuronUnreachable)];

			return null;
		}

		/// <summary>
		/// Checks if an exception means that the token's Neuron couldn't be reached.
		/// </summary>
		/// <param name="Error">The exception that was raised.</param>
		/// <returns>If the Neuron couldn't be reached, or the device is offline.</returns>
		public static bool IsUnreachable(Exception Error)
		{
			Exception Inner = Unwrap(Error);

			if (Inner is TimeoutException ||
				Inner is MissingNetworkException ||
				Inner is RemoteServerTimeoutException ||
				Inner is RemoteServerNotFoundException ||
				Inner is ServiceUnavailableException ||
				Inner is RecipientUnavailableException)
			{
				return true;
			}

			return Inner is not StanzaExceptionException && !ServiceRef.NetworkService.IsOnline;
		}

		/// <summary>
		/// Shows an error from a token action in a dialog: the mapped message when there is one, and the generic error otherwise.
		/// </summary>
		/// <param name="Error">The exception that was raised.</param>
		public static Task ShowAsync(Exception Error)
		{
			string? Message = ToMessage(Error);

			if (Message is null)
				return ServiceRef.UiService.DisplayException(Error);

			return ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Message,
				ServiceRef.Localizer[nameof(AppResources.Ok)]);
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
	}
}
