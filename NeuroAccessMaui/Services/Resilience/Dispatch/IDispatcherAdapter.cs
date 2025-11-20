using System;

namespace NeuroAccessMaui.Services.Resilience.Dispatch
{
    public interface IDispatcherAdapter
    {
        void Post(Action action);
    }
}
