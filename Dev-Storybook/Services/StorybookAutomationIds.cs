using System.Text;

namespace DevStorybook.Services
{
	/// <summary>
	/// Creates stable, human-readable automation identifiers for Storybook navigation and stories.
	/// </summary>
	internal static class StorybookAutomationIds
	{
		/// <summary>
		/// Creates an underscore-separated automation identifier from descriptive parts.
		/// </summary>
		/// <param name="Parts">The descriptive identifier parts.</param>
		/// <returns>A normalized automation identifier.</returns>
		internal static string Create(params string[] Parts)
		{
			StringBuilder Identifier = new StringBuilder();
			foreach (string Part in Parts.Where(Part => !string.IsNullOrWhiteSpace(Part)))
			{
				foreach (char Character in Part)
				{
					if (char.IsLetterOrDigit(Character))
					{
						Identifier.Append(char.ToLowerInvariant(Character));
					}
					else if (Identifier.Length > 0 && Identifier[^1] != '_')
					{
						Identifier.Append('_');
					}
				}

				if (Identifier.Length > 0 && Identifier[^1] != '_')
					Identifier.Append('_');
			}

			return Identifier.ToString().TrimEnd('_');
		}
	}
}
