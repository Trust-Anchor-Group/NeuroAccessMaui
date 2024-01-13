using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.Data
{
	/// <summary>
	/// Static class containing ISO 5218 gender codes
	/// </summary>
	public static class ISO_5218
	{
		/// <summary>
		/// Contains one record of the ISO 5218 data set.
		/// </summary>
		/// <param name="Gender">Gender name</param>
		/// <param name="Code">ISO 5218 designated Code</param>
		/// <param name="Letter">Letter used in Legal IDs GENDER property.</param>
		public class Record(string Gender, int Code, string Letter, string LocalizedNameId)
		{
			/// <summary>
			/// Gender
			/// </summary>
			public string Gender { get; } = Gender;

			/// <summary>
			/// ISO 5218 gender code
			/// </summary>
			public int Code { get; } = Code;

			/// <summary>
			/// Character used in Legal IDs GENDER property.
			/// </summary>
			public string Letter { get; } = Letter;

			public string LocalizedNameId { get; } = LocalizedNameId;
		}

		private static Dictionary<int, Record>? genderByCode = null;
		private static Dictionary<string, Record>? genderByLetter = null;

		/// <summary>
		/// Tries to get the gender label corresponding to an ISO 5218 gender code.
		/// </summary>
		/// <param name="Code">Gender code</param>
		/// <param name="Gender">Gender, if found.</param>
		/// <returns>If a corresponding gender code was found.</returns>
		public static bool CodeToGender(int Code, out Record? Gender)
		{
			if (genderByCode is null)
			{
				Dictionary<int, Record> Temp = [];

				foreach (Record Rec in Data)
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
		public static bool LetterToGender(string Letter, out Record? Gender)
		{
			if (genderByLetter is null)
			{
				Dictionary<string, Record> Temp = [];

				foreach (Record Rec in Data)
					Temp[Rec.Letter] = Rec;

				genderByLetter = Temp;
			}

			return genderByLetter.TryGetValue(Letter, out Gender);
		}

		/// <summary>
		/// Available gender codes
		/// </summary>
		public static readonly Record[] Data =
		[
			new("Male", 1, "M", nameof(AppResources.Male)),
			new("Female", 2, "F", nameof(AppResources.Female)),
			new("Other", 9, "X", nameof(AppResources.Other))
		];
	}
}
