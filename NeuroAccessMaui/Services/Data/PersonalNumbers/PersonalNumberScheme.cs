using Waher.Script;

namespace NeuroAccessMaui.Services.Data.PersonalNumbers
{
	/// <summary>
	/// Checks personal numbers against a personal number scheme.
	/// </summary>
	/// <param name="VariableName">Name of variable to use in script for the personal number.</param>
	/// <param name="DisplayString">A string that can be displayed to a user, informing the user about the approximate format expected.</param>
	/// <param name="Pattern">Expression checking if the scheme applies to a personal number.</param>
	/// <param name="Check">Optional expression, checking if the contents of the personal number is valid.</param>
	/// <param name="Normalize">Optional normalization expression.</param>
	public class PersonalNumberScheme(string VariableName, string DisplayString, Expression Pattern, Expression? Check,
		Expression? Normalize)
	{
		private readonly string variableName = VariableName;
		private readonly Expression pattern = Pattern;
		private readonly Expression? check = Check;
		private readonly Expression? normalize = Normalize;

		/// <summary>
		/// A string that can be displayed to a user, informing the user about the approximate format expected.
		/// </summary>
		public string DisplayString { get; } = DisplayString;

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
