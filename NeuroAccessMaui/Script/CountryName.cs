using System.Threading.Tasks;
using Waher.Script;
using ScriptElements = Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.Script
{
	/// <summary>
	/// Looks up a Country Code and returns the corresponding Country Name.
	/// </summary>
	public class CountryName : FunctionOneScalarVariable
	{
		/// <summary>
		/// Looks up a Country Code and returns the corresponding Country Name.
		/// </summary>
		/// <param name="Argument">Argument.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public CountryName(ScriptNode Argument, int Start, int Length, Expression Expression)
			: base(Argument, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(CountryName);

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override ScriptElements.IElement EvaluateScalar(string Argument, Variables Variables)
		{
			if (string.IsNullOrEmpty(Argument))
				return new StringValue(string.Empty);
			else if (ISO_3166_1.TryGetCountryByCode(Argument, out ISO_3166_Country? Country))
				return new StringValue(Country.Name);
			else
				throw new ScriptRuntimeException("Country Code not recognized: " + Argument, this);
		}

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override Task<ScriptElements.IElement> EvaluateScalarAsync(string Argument, Variables Variables)
		{
			return Task.FromResult(this.EvaluateScalar(Argument, Variables));
		}
	}
}
