using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.Data
{
	/// <summary>
	/// Static class containing ISO 5218 gender codes
	/// </summary>
	public static partial class ISO_5218
	{

		private static Dictionary<int, ISO_5218_Gender>? genderByCode = null;
		private static Dictionary<string, ISO_5218_Gender>? genderByLetter = null;

		/// <summary>
		/// Tries to get the gender label corresponding to an ISO 5218 gender code.
		/// </summary>
		/// <param name="Code">Gender code</param>
		/// <param name="Gender">Gender, if found.</param>
		/// <returns>If a corresponding gender code was found.</returns>
		public static bool CodeToGender(int Code, out ISO_5218_Gender? Gender)
		{
			if (genderByCode is null)
			{
				Dictionary<int, ISO_5218_Gender> Temp = [];

				foreach (ISO_5218_Gender Rec in Data)
					Temp[Rec.Code] = Rec;

				genderByCode = Temp;
			}

			return genderByCode.TryGetValue(Code, out Gender);
		}

		/// <summary>
		/// Tries to get the gender label corresponding to an ISO 5218 gender code.
		/// </summary>
		/// <param name="Letter">Gender Letter</param>
		/// <param name="Gender">Gender, if found.</param>
		/// <returns>If a corresponding gender letter was found.</returns>
		public static bool LetterToGender(string Letter, out ISO_5218_Gender? Gender)
		{
			if (genderByLetter is null)
			{
				Dictionary<string, ISO_5218_Gender> Temp = [];

				foreach (ISO_5218_Gender Rec in Data)
					Temp[Rec.Letter] = Rec;

				genderByLetter = Temp;
			}

			return genderByLetter.TryGetValue(Letter, out Gender);
		}

		/// <summary>
		/// Available gender codes
		/// </summary>
		public static readonly ISO_5218_Gender[] Data =
		[
			new("Male", 1, "M", nameof(AppResources.Male), '♂'),
			new("Female", 2, "F", nameof(AppResources.Female), '♀'),
			new("Other", 9, "X", nameof(AppResources.Other), '⚥')
		];
	}
}
