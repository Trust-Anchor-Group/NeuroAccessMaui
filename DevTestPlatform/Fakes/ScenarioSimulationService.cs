using DevTestPlatform.Catalog;
using DevTestPlatform.Resources;

namespace DevTestPlatform.Fakes;

/// <summary>Simulates developer status outcomes without claiming to execute production backend operations.</summary>
public sealed class ScenarioSimulationService
{
    /// <summary>Returns the selected synthetic outcome, or holds loading until the session is cancelled.</summary>
    /// <param name="Scenario">The selected controlled state.</param>
    /// <param name="CancellationToken">The owning preview's cancellation token.</param>
    /// <returns>The localized simulated status.</returns>
    public async Task<string> RunAsync(ScenarioDefinition Scenario, CancellationToken CancellationToken)
    {
        if (Scenario.IsBusy)
            await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken);
        CancellationToken.ThrowIfCancellationRequested();
        return Scenario.OutcomeKey.Length > 0 ? DevText.Get(Scenario.OutcomeKey) : DevText.Get("SimulationComplete");
    }
}
