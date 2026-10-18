using NeuroAccessMaui.UITests.Models;

namespace NeuroAccessMaui.UITests.Screens.Kyc;

/// <summary>Checks every required mapped summary value before signing the application.</summary>
internal sealed class KycSummaryScreen
{
	private readonly MobileUi ui;
	internal KycSummaryScreen(UiSession Session) => this.ui = Session.Ui;

	internal void AssertMatches(PersonalIdentityData Data, bool IncludesMiddleName)
	{
		string Screen = AutomationIds.ControlKycProcessSummary;
		this.ui.Find(Screen);
		Dictionary<string, HashSet<string>> Pending = Data.SummaryValues(IncludesMiddleName);
		int Scrolls = 0;
		for (; Scrolls <= 48; Scrolls++)
		{
			foreach (string Mapping in Pending.Keys.ToArray())
			{
				string Id = AutomationIds.ForKey(AutomationIds.LabelKycSummaryValue, Mapping);
				if (!this.ui.Visible(Id, Screen))
					continue;
				string Actual = this.ui.Read(Id, Screen);
				Assert.IsTrue(Pending[Mapping].Contains(Actual),
					$"Summary {Mapping} shows '{Actual}', expected one of: {string.Join(", ", Pending[Mapping].Select(Value => "'" + Value + "'"))}.");
				Pending.Remove(Mapping);
			}
			if (Pending.Count == 0 || Scrolls == 48)
				break;
			this.ui.Scroll(Screen, Fraction: 0.45);
		}
		Assert.AreEqual(0, Pending.Count, "KYC summary is missing mappings: " + string.Join(", ", Pending.Keys) + ".");
		// Return to the header so the sent panel will be visible after submission.
		for (int i = 0; i <= Scrolls; i++)
			this.ui.Scroll(Screen, Down: false, Fraction: 0.45);
	}
}
