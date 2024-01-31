namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// Rule Range model
	/// </summary>
	public class RuleRangeModel
	{
		/// <summary>
		/// Rule Range model
		/// </summary>
		/// <param name="RuleRange">Rule Range</param>
		/// <param name="Label">Label</param>
		public RuleRangeModel(object RuleRange, string Label)
		{
			this.RuleRange = RuleRange;
			this.Label = Label;
		}

		/// <summary>
		/// Rule Range
		/// </summary>
		public object RuleRange { get; }

		/// <summary>
		/// Label
		/// </summary>
		public string Label { get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Label;
		}
	}
}
