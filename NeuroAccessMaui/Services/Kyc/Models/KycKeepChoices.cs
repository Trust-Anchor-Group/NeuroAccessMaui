using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// A set of optional retained identity properties that the user keeps or leaves out of the final identity together.
	/// </summary>
	/// <param name="Id">The group identifier. This is the summary mapping of the group, such as <c>BDATE</c> for the birth date parts.</param>
	/// <param name="PropertyKeys">The identity property keys in the group.</param>
	public sealed record KycKeepChoiceGroup(string Id, IReadOnlyList<string> PropertyKeys);

	/// <summary>
	/// Reads and writes the user's choices about which optional retained properties stay on the final identity.
	/// </summary>
	/// <remarks>
	/// Choices are stored as reserved values in <see cref="KycProcess.Values"/>, so they are saved and restored with the other
	/// process values. A property without a stored choice uses the default from the process content policy.
	/// </remarks>
	public static class KycKeepChoices
	{
		private const string valueKeyPrefix = "application.keep.";

		private static readonly string[] birthDateKeys =
		[
			Constants.XmppProperties.BirthDay,
			Constants.XmppProperties.BirthMonth,
			Constants.XmppProperties.BirthYear
		];

		/// <summary>
		/// Determines whether a process value key holds a keep choice rather than a field value.
		/// </summary>
		/// <param name="ValueKey">The process value key.</param>
		/// <returns><c>true</c> if the key holds a keep choice; otherwise, <c>false</c>.</returns>
		public static bool IsChoiceValueKey(string? ValueKey)
		{
			return ValueKey?.StartsWith(valueKeyPrefix, StringComparison.OrdinalIgnoreCase) == true;
		}

		/// <summary>
		/// Determines whether the user may leave a property out of the final identity.
		/// </summary>
		/// <param name="Process">The KYC process.</param>
		/// <param name="PropertyKey">The identity property key.</param>
		/// <returns><c>true</c> if the property is an optional retained property; otherwise, <c>false</c>.</returns>
		public static bool IsOptional(KycProcess Process, string PropertyKey)
		{
			return Process.ApplicationPolicy.Content.OptionalKeepDefaults.ContainsKey(PropertyKey);
		}

		/// <summary>
		/// Determines whether a property stays off the final identity unless the user chooses to keep it.
		/// </summary>
		/// <param name="Process">The KYC process.</param>
		/// <param name="PropertyKey">The identity property key.</param>
		/// <returns><c>true</c> if the property is optional and left out by default; otherwise, <c>false</c>.</returns>
		public static bool IsLeftOutByDefault(KycProcess Process, string PropertyKey)
		{
			return Process.ApplicationPolicy.Content.OptionalKeepDefaults.TryGetValue(PropertyKey, out bool DefaultKept) && !DefaultKept;
		}

		/// <summary>
		/// Determines whether a retained property will be kept on the final identity.
		/// </summary>
		/// <param name="Process">The KYC process.</param>
		/// <param name="PropertyKey">The identity property key.</param>
		/// <returns><c>true</c> unless the property is optional and the user, or its default, leaves it out.</returns>
		public static bool IsKept(KycProcess Process, string PropertyKey)
		{
			if (!Process.ApplicationPolicy.Content.OptionalKeepDefaults.TryGetValue(PropertyKey, out bool DefaultKept))
				return true;

			if (Process.Values.TryGetValue(GetValueKey(PropertyKey), out string? Stored) &&
				bool.TryParse(Stored, out bool Kept))
			{
				return Kept;
			}

			return DefaultKept;
		}

		/// <summary>
		/// Records whether optional retained properties should be kept on the final identity.
		/// </summary>
		/// <param name="Process">The KYC process.</param>
		/// <param name="PropertyKeys">The identity property keys to update. Keys that are not optional are ignored.</param>
		/// <param name="Kept">Whether the properties should be kept.</param>
		public static void SetKept(KycProcess Process, IEnumerable<string> PropertyKeys, bool Kept)
		{
			foreach (string PropertyKey in PropertyKeys)
			{
				if (IsOptional(Process, PropertyKey))
					Process.Values[GetValueKey(PropertyKey)] = Kept.ToString(CultureInfo.InvariantCulture);
			}
		}

		/// <summary>
		/// Gets the optional retained properties that will be left out of the final identity.
		/// </summary>
		/// <param name="Process">The KYC process.</param>
		/// <returns>The property keys to leave out.</returns>
		public static IReadOnlyList<string> GetExcludedPropertyKeys(KycProcess Process)
		{
			return Process.ApplicationPolicy.Content.OptionalKeepDefaults.Keys
				.Where(Key => !IsKept(Process, Key))
				.ToList();
		}

		/// <summary>
		/// Gets the optional retained properties as the groups the user chooses between, in policy order.
		/// </summary>
		/// <remarks>
		/// The birth date parts are offered as one choice, since the summary shows them as one date.
		/// </remarks>
		/// <param name="Process">The KYC process.</param>
		/// <returns>The choice groups.</returns>
		public static IReadOnlyList<KycKeepChoiceGroup> GetChoiceGroups(KycProcess Process)
		{
			List<KycKeepChoiceGroup> Groups = new List<KycKeepChoiceGroup>();
			bool BirthDateAdded = false;

			foreach (string Key in Process.ApplicationPolicy.Content.KeepPropertyKeys)
			{
				if (!IsOptional(Process, Key))
					continue;

				if (birthDateKeys.Contains(Key, StringComparer.OrdinalIgnoreCase))
				{
					if (BirthDateAdded)
						continue;

					BirthDateAdded = true;
					string[] OptionalBirthDateKeys = birthDateKeys.Where(BirthDateKey => IsOptional(Process, BirthDateKey)).ToArray();
					Groups.Add(new KycKeepChoiceGroup(Constants.CustomXmppProperties.BirthDate, OptionalBirthDateKeys));
					continue;
				}

				Groups.Add(new KycKeepChoiceGroup(Key, [Key]));
			}

			return Groups;
		}

		private static string GetValueKey(string PropertyKey)
		{
			return valueKeyPrefix + PropertyKey.Trim().ToUpperInvariant();
		}
	}
}
