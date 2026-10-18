namespace NeuroAccessMaui.UnitTests.Contracts;

/// <summary>Protects dynamic selectors against collisions across records and schema keys.</summary>
[TestClass, TestCategory("Unit")]
public sealed class AutomationIdsTests
{
	/// <summary>Keeps punctuation, Unicode and key boundaries distinct in native selectors.</summary>
	[TestMethod]
	public void Test_01_EncodeDistinctRecordKeys()
	{
		const string Prefix = AutomationIds.ItemContactsListContact;
		string[] Identifiers =
		[
			AutomationIds.ForKey(Prefix, "a-b"), AutomationIds.ForKey(Prefix, "a_b"),
			AutomationIds.ForKey(Prefix, "ab"), AutomationIds.ForKey(Prefix, "a", "b"),
			AutomationIds.ForKey(Prefix, "é"), AutomationIds.ForKey(Prefix, "e")
		];
		Assert.AreEqual(Identifiers.Length, Identifiers.Distinct(StringComparer.Ordinal).Count());
		Assert.AreEqual(Prefix + "_key_c3a9", AutomationIds.ForKey(Prefix, "é"));
	}

	/// <summary>Rejects fixed controls and absent keys as dynamic selector namespaces.</summary>
	[TestMethod]
	public void Test_02_RejectInvalidDynamicSelector()
	{
		Assert.ThrowsExactly<ArgumentException>(() => AutomationIds.ForKey(AutomationIds.ScreenMainHome, "record"));
		Assert.ThrowsExactly<ArgumentException>(() => AutomationIds.ForKey(AutomationIds.ItemContactsListContact));
		Assert.ThrowsExactly<ArgumentException>(() => AutomationIds.ForKey(AutomationIds.ItemContactsListContact, ""));
	}
}
