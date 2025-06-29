using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace NeuroAccessMaui.Services.Localization
{
	public interface IReportingStringLocalizer : IStringLocalizer
	{

		LocalizedString this[string Name, bool ShouldReport] { get; }

		LocalizedString this[string Name, bool ShouldReport, params object[] Arguments] { get; }
	}

	/// <summary>
	/// Localizer, that reports missing strings to the operator of the corrected broker, via the event log.
	/// </summary>
	/// <param name="Localizer">Base localizer.</param>
	public class ReportingStringLocalizer(IStringLocalizer Localizer) : IReportingStringLocalizer
	{
		private readonly IStringLocalizer localizer = Localizer;

		public LocalizedString this[string Name]
			=> ((IReportingStringLocalizer)this)[Name, true];

		public LocalizedString this[string Name, params object[] Arguments]
			=> ((IReportingStringLocalizer)this)[Name, true, Arguments];

		LocalizedString IReportingStringLocalizer.this[string Name, bool ShouldReport]
		{
			get
			{
				LocalizedString Result = this.localizer[Name];
				if (Result is not null && !Result.ResourceNotFound)
					return Result;

				StackTrace Trace = new();
				Type Caller = typeof(ServiceRef);
				int i, c = Trace.FrameCount;
				Assembly ThisAssembly = typeof(App).Assembly;

				for (i = 1; i < c; i++)
				{
					Type? T = Trace.GetFrame(i)?.GetMethod()?.DeclaringType;
					if (T is null)
						continue;

					if (T.Assembly.FullName == ThisAssembly.FullName)
					{
						if (T == typeof(ReportingStringLocalizer))
							continue;

						if (T.IsConstructedGenericType)
							T = T.GetGenericTypeDefinition();

						Caller = T;
						break;
					}
				}

				if (ShouldReport)
					LocalizeExtension.ReportMissingString(Name, Caller);

				return new LocalizedString(Name, Name, true);
			}
		}

		LocalizedString IReportingStringLocalizer.this[string Name, bool ShouldReport, params object[] Arguments]
		{
			get
			{
				LocalizedString Result = this[Name];
				if (Result.ResourceNotFound)
					return Result;

				return new LocalizedString(Name, string.Format(CultureInfo.CurrentCulture, Result.Value, Arguments));
			}
		}

		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
		{
			return this.localizer.GetAllStrings(includeParentCultures);
		}
	}
}
