using Waher.Script;
using ScriptElements = Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.Script
{
	/// <summary>
	/// Returns an object whose property names are country codes, and their corresponding 
	/// values the corresponding country name.
	/// </summary>
	public class CountryEntries : FunctionZeroVariables
	{
		/// <summary>
		/// Returns a vector of recognized country codes.
		/// </summary>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public CountryEntries(int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(CountryEntries);

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="ScriptNode.IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override ScriptElements.IElement Evaluate(Variables Variables)
		{
			ISO_3166_Country[] Countries = ISO_3166_1.Countries;
			int i, c = Countries.Length;
			Dictionary<string, ScriptElements.IElement> Result = [];
			ISO_3166_Country Country;

			for (i = 0; i < c; i++)
			{
				Country = Countries[i];
				Result[Country.Alpha2] = new StringValue(Country.Name);
			}

			return new ObjectValue(Result);
		}
	}
}
