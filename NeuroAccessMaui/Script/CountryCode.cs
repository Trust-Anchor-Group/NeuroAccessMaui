using Waher.Script;
using ScriptElements = Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.Script
{
    /// <summary>
    /// Looks up a Country Name and returns the corresponding Country Code.
    /// </summary>
    public class CountryCode : FunctionOneScalarVariable
    {
		/// <summary>
		/// Looks up a Country Name and returns the corresponding Country Code.
		/// </summary>
		/// <param name="Argument">Argument.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public CountryCode(ScriptNode Argument, int Start, int Length, Expression Expression)
            : base(Argument, Start, Length, Expression)
        {
        }

        /// <summary>
        /// Name of the function
        /// </summary>
        public override string FunctionName => nameof(CountryCode);

        /// <summary>
        /// Evaluates the function on a scalar argument.
        /// </summary>
        /// <param name="Argument">Function argument.</param>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Function result.</returns>
        public override ScriptElements.IElement EvaluateScalar(string Argument, Variables Variables)
        {
			if (ISO_3166_1.TryGetCountryByName(Argument, out ISO_3166_Country? Country))
				return new StringValue(Country.Alpha2);
			else
				throw new ScriptRuntimeException("Country not recognized: " + Argument, this);
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
