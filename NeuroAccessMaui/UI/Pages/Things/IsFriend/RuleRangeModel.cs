namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// Rule Range model
	/// </summary>
	/// <param name="RuleRange">Rule Range</param>
	/// <param name="Label">Label</param>
	public class RuleRangeModel(object RuleRange, string Label)
	{

		/// <summary>
		/// Rule Range
		/// </summary>
		public object RuleRange { get; } = RuleRange;

		/// <summary>
		/// Label
		/// </summary>
		public string Label { get; } = Label;

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Label;
		}
	}
}
