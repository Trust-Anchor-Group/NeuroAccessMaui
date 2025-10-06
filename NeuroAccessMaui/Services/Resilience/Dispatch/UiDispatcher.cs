using System;
using Microsoft.Maui.ApplicationModel;

namespace NeuroAccessMaui.Services.Resilience.Dispatch
{
    public sealed class UiDispatcher : IDispatcherAdapter
    {
        public static UiDispatcher Instance { get; } = new UiDispatcher();
        private UiDispatcher() { }

        public void Post(Action action)
        {
            if (action is null) return;
            MainThread.BeginInvokeOnMainThread(action);
        }
    }
}
