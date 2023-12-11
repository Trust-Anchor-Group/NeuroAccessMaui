using Waher.Script;

namespace NeuroAccessMaui.Services.Data.PersonalNumbers
{
	/// <summary>
	/// Checks personal numbers against a personal number scheme.
	/// </summary>
	public class PersonalNumberScheme
	{
		private readonly string variableName;
		private readonly Expression pattern;
		private readonly Expression check;
		private readonly Expression normalize;

		/// <summary>
		/// Checks personal numbers against a personal number scheme.
		/// </summary>
		/// <param name="variableName">Name of variable to use in script for the personal number.</param>
		/// <param name="displayString">A string that can be displayed to a user, informing the user about the approximate format expected.</param>
		/// <param name="pattern">Expression checking if the scheme applies to a personal number.</param>
		/// <param name="check">Optional expression, checking if the contents of the personal number is valid.</param>
		/// <param name="normalize">Optional normalization expression.</param>
		public PersonalNumberScheme(string variableName, string displayString, Expression pattern, Expression check, Expression normalize)
		{
			this.variableName = variableName;
			this.DisplayString = displayString;
			this.pattern = pattern;
			this.check = check;
			this.normalize = normalize;
		}

		/// <summary>
		/// A string that can be displayed to a user, informing the user about the approximate format expected.
		/// </summary>
		public string DisplayString { get; }

		/// <summary>
		/// Checks if a personal number is valid according to the personal number scheme.
		/// </summary>
		/// <returns>Validation information about the number.</returns>
		public async Task<NumberInformation> Validate(string PersonalNumber)
		{
			NumberInformation Info = new()
			{
				PersonalNumber = PersonalNumber,
				DisplayString = string.Empty
			};

			try
			{
				Variables Variables = new(new Variable(this.variableName, PersonalNumber));
				object EvalResult = await this.pattern.EvaluateAsync(Variables);

				if (EvalResult is bool b)
				{
					if (!b)
					{
						Info.IsValid = null;
						return Info;
					}

					if (this.check is not null)
					{
						EvalResult = await this.check.EvaluateAsync(Variables);

						if (EvalResult is not bool b2 || !b2)
						{
							Info.IsValid = false;
							return Info;
						}
					}

					if (this.normalize is not null)
					{
						EvalResult = await this.normalize.EvaluateAsync(Variables);

						if (EvalResult is not string Normalized)
						{
							Info.IsValid = false;
							return Info;
						}

						Info.PersonalNumber = Normalized;
					}

					Info.IsValid = true;
					return Info;
				}
				else
				{
					Info.IsValid = null;
					return Info;
				}
			}
			catch (Exception)
			{
				Info.IsValid = false;
				return Info;
			}
		}

	}
}
