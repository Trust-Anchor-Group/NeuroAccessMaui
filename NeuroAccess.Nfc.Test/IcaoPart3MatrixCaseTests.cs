using System.Reflection;
using NeuroAccess.Nfc.Test.IcaoPart3;

namespace NeuroAccess.Nfc.Test
{
	/// <summary>
	/// Executes each ICAO Part 3 matrix case as a dynamic row.
	/// </summary>
	[TestClass]
	public class IcaoPart3MatrixCaseTests
	{
		/// <summary>
		/// Gets the ICAO Part 3 matrix cases used as data rows.
		/// </summary>
		/// <returns>Matrix case rows.</returns>
		public static IEnumerable<object[]> GetPart3Cases()
		{
			return IcaoPart3TestData.LoadMatrix().Cases
				.Select(TestCase => new object[] { TestCase });
		}

		/// <summary>
		/// Gets a readable display name for an ICAO Part 3 case row.
		/// </summary>
		/// <param name="MethodInfo">Test method information.</param>
		/// <param name="Data">Dynamic data row.</param>
		/// <returns>Display name.</returns>
		public static string GetPart3CaseDisplayName(MethodInfo MethodInfo, object[] Data)
		{
			if (Data.Length > 0 && Data[0] is IcaoPart3Case TestCase)
				return TestCase.Id + " - " + TestCase.Category;

			return MethodInfo.Name;
		}

		/// <summary>
		/// Executes one ICAO Part 3 matrix case.
		/// </summary>
		/// <param name="TestCase">Matrix test case.</param>
		[TestMethod]
		[DynamicData(nameof(GetPart3Cases), DynamicDataDisplayName = nameof(GetPart3CaseDisplayName))]
		public void Test_01_IcaoPart3_Case(IcaoPart3Case TestCase)
		{
			IcaoPart3CoverageClassifier Classifier = IcaoPart3TestData.CreateCoverageClassifier();
			IcaoPart3CoverageResult Result = Classifier.Classify(TestCase);

			if (Result.Status == IcaoPart3CoverageStatus.Failed)
				Assert.Fail(Result.CaseId + ": " + Result.Reason);

			if (Result.Status != IcaoPart3CoverageStatus.Passed)
				Assert.Inconclusive(Result.Status + ": " + Result.Reason);
		}
	}
}
