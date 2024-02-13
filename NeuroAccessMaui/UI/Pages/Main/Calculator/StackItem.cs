namespace NeuroAccessMaui.UI.Pages.Main.Calculator
{
	/// <summary>
	/// Binary operator priority
	/// </summary>
	public enum OperatorPriority
	{
		/// <summary>
		/// Evaluation operator
		/// </summary>
		Equals = 0,

		/// <summary>
		/// Parenthesis operator
		/// </summary>
		Parenthesis = 1,

		/// <summary>
		/// Addition and Subtraction
		/// </summary>
		Terms = 2,

		/// <summary>
		/// Multiplication and division
		/// </summary>
		Factors = 3,

		/// <summary>
		/// Powers and exponents
		/// </summary>
		Powers = 4
	}

	/// <summary>
	/// Calculation stack item
	/// </summary>
	public class StackItem
	{
		/// <summary>
		/// Entry
		/// </summary>
		public string? Entry;

		/// <summary>
		/// Corresponding script
		/// </summary>
		public string? Script;

		/// <summary>
		/// Priority level
		/// </summary>
		public OperatorPriority Priority;

		/// <summary>
		/// Operator to display
		/// </summary>
		public string? Operator;

		/// <summary>
		/// If parenthesis was started
		/// </summary>
		public bool StartParenthesis;
	}
}
