using System.Diagnostics;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Writes navigation and page lifecycle breadcrumbs through the existing <see cref="ILogService"/> pipeline.
	/// </summary>
	/// <remarks>
	/// Breadcrumbs carry page names, route names, and durations only. Navigation arguments, query strings, URIs, and
	/// exception messages are never logged, because they can carry personal data or key material. Logging failures are
	/// swallowed so that telemetry can never break navigation.
	/// </remarks>
	internal static class UiTelemetry
	{
		/// <summary>
		/// Logs that a navigation has started.
		/// </summary>
		/// <param name="LogService">The log service to write to.</param>
		/// <param name="Action">The navigation action, such as <c>GoTo</c> or <c>GoBack</c>.</param>
		/// <param name="SourcePage">The page shown before the navigation, if any.</param>
		/// <param name="TargetPage">The page being navigated to, if known.</param>
		/// <param name="Route">The requested route, if the navigation is route based.</param>
		/// <param name="BackMethod">The back method of the navigation, if any.</param>
		/// <returns>A timestamp to pass to <see cref="LogNavigationCompleted"/> or <see cref="LogNavigationFailed"/>.</returns>
		internal static long LogNavigationStarted(ILogService LogService, string Action, BaseContentPage? SourcePage,
			BaseContentPage? TargetPage, string? Route = null, BackMethod? BackMethod = null)
		{
			long StartTimestamp = Stopwatch.GetTimestamp();
			SafeLog(() => LogService.LogDebug("NavigationStarted",
				CreateNavigationTags(Action, SourcePage, TargetPage, Route, BackMethod)));
			return StartTimestamp;
		}

		/// <summary>
		/// Logs that a navigation has completed and the target page has appeared.
		/// </summary>
		/// <param name="LogService">The log service to write to.</param>
		/// <param name="Action">The navigation action, such as <c>GoTo</c> or <c>GoBack</c>.</param>
		/// <param name="SourcePage">The page shown before the navigation, if any.</param>
		/// <param name="TargetPage">The page navigated to.</param>
		/// <param name="StartTimestamp">The timestamp returned by <see cref="LogNavigationStarted"/>.</param>
		/// <param name="Route">The requested route, if the navigation is route based.</param>
		/// <param name="BackMethod">The back method of the navigation, if any.</param>
		internal static void LogNavigationCompleted(ILogService LogService, string Action, BaseContentPage? SourcePage,
			BaseContentPage? TargetPage, long StartTimestamp, string? Route = null, BackMethod? BackMethod = null)
		{
			SafeLog(() => LogService.LogDebug("NavigationCompleted",
				CreateNavigationTags(Action, SourcePage, TargetPage, Route, BackMethod,
					CreateDurationTag(StartTimestamp))));
		}

		/// <summary>
		/// Logs that a navigation has failed.
		/// </summary>
		/// <param name="LogService">The log service to write to.</param>
		/// <param name="Action">The navigation action, such as <c>GoTo</c> or <c>GoBack</c>.</param>
		/// <param name="Exception">The exception that made the navigation fail. Only its type is logged.</param>
		/// <param name="SourcePage">The page shown before the navigation, if any.</param>
		/// <param name="TargetPage">The page being navigated to, if known.</param>
		/// <param name="StartTimestamp">The timestamp returned by <see cref="LogNavigationStarted"/>.</param>
		/// <param name="Route">The requested route, if the navigation is route based.</param>
		/// <param name="BackMethod">The back method of the navigation, if any.</param>
		internal static void LogNavigationFailed(ILogService LogService, string Action, Exception Exception,
			BaseContentPage? SourcePage, BaseContentPage? TargetPage, long StartTimestamp, string? Route = null,
			BackMethod? BackMethod = null)
		{
			SafeLog(() => LogService.LogWarning("NavigationFailed",
				CreateNavigationTags(Action, SourcePage, TargetPage, Route, BackMethod,
					CreateDurationTag(StartTimestamp),
					new KeyValuePair<string, object?>("ExceptionType", Exception.GetType().Name))));
		}

		/// <summary>
		/// Logs that a page has appeared.
		/// </summary>
		/// <param name="LogService">The log service to write to.</param>
		/// <param name="Page">The page that appeared.</param>
		/// <param name="Trigger">What made the page appear, such as a navigation action or startup.</param>
		internal static void LogPageAppeared(ILogService LogService, BaseContentPage Page, string Trigger)
		{
			SafeLog(() => LogService.LogDebug("PageAppeared", CreatePageTags(Page, Trigger)));
		}

		/// <summary>
		/// Logs that a page lifecycle step has failed.
		/// </summary>
		/// <param name="LogService">The log service to write to.</param>
		/// <param name="Page">The page whose lifecycle step failed.</param>
		/// <param name="Phase">The lifecycle phase, such as <c>Appearing</c>, <c>Disappearing</c>, or <c>Cleanup</c>.</param>
		/// <param name="Trigger">What ran the lifecycle step, such as a navigation action or startup.</param>
		/// <param name="Exception">The exception raised by the step. Only its type is logged.</param>
		internal static void LogPageLifecycleFailed(ILogService LogService, BaseContentPage Page, string Phase, string Trigger,
			Exception Exception)
		{
			SafeLog(() => LogService.LogWarning("PageLifecycleFailed", CreatePageTags(Page, Trigger,
				new KeyValuePair<string, object?>("Phase", Phase),
				new KeyValuePair<string, object?>("ExceptionType", Exception.GetType().Name))));
		}

		private static KeyValuePair<string, object?>[] CreateNavigationTags(string Action, BaseContentPage? SourcePage,
			BaseContentPage? TargetPage, string? Route, BackMethod? BackMethod, params KeyValuePair<string, object?>[] AdditionalTags)
		{
			List<KeyValuePair<string, object?>> Tags =
			[
				new KeyValuePair<string, object?>("Action", Action),
				new KeyValuePair<string, object?>("SourcePage", GetPageName(SourcePage)),
				new KeyValuePair<string, object?>("TargetPage", GetPageName(TargetPage)),
				new KeyValuePair<string, object?>("Route", Route is null ? GetRouteName(TargetPage) : GetRouteName(Route))
			];

			if (BackMethod.HasValue)
				Tags.Add(new KeyValuePair<string, object?>("BackMethod", BackMethod.Value.ToString()));

			if (AdditionalTags.Length > 0)
				Tags.AddRange(AdditionalTags);

			return [.. Tags];
		}

		private static KeyValuePair<string, object?>[] CreatePageTags(BaseContentPage Page, string Trigger,
			params KeyValuePair<string, object?>[] AdditionalTags)
		{
			List<KeyValuePair<string, object?>> Tags =
			[
				new KeyValuePair<string, object?>("Page", GetPageName(Page)),
				new KeyValuePair<string, object?>("Route", GetRouteName(Page)),
				new KeyValuePair<string, object?>("Trigger", Trigger)
			];

			if (AdditionalTags.Length > 0)
				Tags.AddRange(AdditionalTags);

			return [.. Tags];
		}

		private static KeyValuePair<string, object?> CreateDurationTag(long StartTimestamp)
		{
			long DurationMs = (long)Stopwatch.GetElapsedTime(StartTimestamp).TotalMilliseconds;
			return new KeyValuePair<string, object?>("DurationMs", DurationMs);
		}

		private static string GetPageName(BaseContentPage? Page)
		{
			return Page?.GetType().Name ?? string.Empty;
		}

		private static string GetRouteName(BaseContentPage? Page)
		{
			if (Page is null)
				return string.Empty;

			return GetRouteName(Routing.GetRoute(Page) ?? Page.GetType().Name);
		}

		/// <summary>
		/// Reduces a route to its last path segment, dropping any query string so that no parameters are logged.
		/// </summary>
		/// <param name="Route">The route to reduce.</param>
		/// <returns>The route name.</returns>
		private static string GetRouteName(string Route)
		{
			string Trimmed = Route.Trim();
			int QueryIndex = Trimmed.IndexOfAny(['?', '&', '#']);
			if (QueryIndex >= 0)
				Trimmed = Trimmed[..QueryIndex];

			Trimmed = Trimmed.Trim('/');
			int SlashIndex = Trimmed.LastIndexOf('/');
			if (SlashIndex >= 0 && SlashIndex + 1 < Trimmed.Length)
				Trimmed = Trimmed[(SlashIndex + 1)..];

			return Trimmed;
		}

		private static void SafeLog(Action LogAction)
		{
			try
			{
				LogAction();
			}
			catch (Exception Exception)
			{
				Debug.WriteLine(Exception);
			}
		}
	}
}
