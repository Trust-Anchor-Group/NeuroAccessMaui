using System;

namespace NeuroAccessMaui.Services.Resilience.Dispatch
{
    public sealed class ImmediateDispatcher : IDispatcherAdapter
    {
        public static ImmediateDispatcher Instance { get; } = new ImmediateDispatcher();
        private ImmediateDispatcher() { }

        public void Post(Action action)
        {
            action?.Invoke();
        }
    }
}
