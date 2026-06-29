using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Services.Kyc.Actions
{
	/// <summary>
	/// Resolves KYC page action names to built-in action handlers.
	/// </summary>
	public class KycActionRegistry
	{
		private readonly Dictionary<string, IKycPageAction> actions;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycActionRegistry"/> class.
		/// </summary>
		/// <param name="Actions">The registered KYC page actions.</param>
		public KycActionRegistry(IEnumerable<IKycPageAction> Actions)
		{
			this.actions = new Dictionary<string, IKycPageAction>(StringComparer.OrdinalIgnoreCase);

			foreach (IKycPageAction Action in Actions)
			{
				string ActionName = Action.Name.Trim();

				if (string.IsNullOrEmpty(ActionName))
				{
					throw new InvalidOperationException("KYC page actions must declare a non-empty name.");
				}

				if (this.actions.ContainsKey(ActionName))
				{
					throw new InvalidOperationException($"Duplicate KYC page action name '{ActionName}'.");
				}

				this.actions.Add(ActionName, Action);
			}
		}

		/// <summary>
		/// Resolves a metadata action name to a built-in action.
		/// </summary>
		/// <param name="ActionName">The metadata action name.</param>
		/// <returns>The matching action, an unsupported-action fallback, or null when no action is declared.</returns>
		public IKycPageAction? Resolve(string? ActionName)
		{
			string Name = ActionName?.Trim() ?? string.Empty;

			if (string.IsNullOrEmpty(Name))
			{
				return null;
			}

			if (this.actions.TryGetValue(Name, out IKycPageAction? Action))
			{
				return Action;
			}

			return new UnsupportedKycPageAction(Name);
		}

		private sealed class UnsupportedKycPageAction : IKycPageAction
		{
			public UnsupportedKycPageAction(string Name)
			{
				this.Name = Name;
			}

			public string Name { get; }

			public Task<KycPageActionState> GetStateAsync(KycPageActionContext Context)
			{
				KycPageActionState State = new KycPageActionState
				{
					IsEnabled = false,
					Title = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					Description = ServiceRef.Localizer[nameof(AppResources.ServiceUnavailable)],
					StatusText = ServiceRef.Localizer[nameof(AppResources.ServiceUnavailable)],
					ErrorText = ServiceRef.Localizer[nameof(AppResources.ServiceUnavailable)]
				};

				return Task.FromResult(State);
			}

			public Task ExecuteAsync(KycPageActionContext Context)
			{
				return Task.CompletedTask;
			}
		}
	}
}
