using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Linq;
using Waher.Runtime.Inventory;
#pragma warning disable CS0618, CS8600, CS8602, CS8603, CS8604, CS8765

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Default implementation of <see cref="INavigationService"/> that uses MAUI Routing for route-based page instantiation.
    /// </summary>
    [Singleton]
    public class NavigationService : INavigationService
    {
        private readonly Stack<ContentPage> navigationStack = new();
        private readonly Stack<ContentPage> modalStack = new();
        private readonly Stack<NavigationArgs?> navigationArgsStack = new();

        private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
        private bool isExecutingTasks = false;
        private NavigationArgs? latestArguments = null;
        // Map of route names to their NavigationArgs for PopLatestArgs lookup
        private readonly Dictionary<string, NavigationArgs> navigationArgsMap = new Dictionary<string, NavigationArgs>();

        /// <summary>
        /// Records navigation arguments for a given route.
        /// </summary>
        private void PushArgs(string Route, NavigationArgs Args)
        {
            this.latestArguments = Args;
            if (TryGetPageName(Route, out string PageName))
            {
                if (Args is not null)
                {
                string? uniqueId = Args.UniqueId;
                    if (!string.IsNullOrEmpty(uniqueId))
                        PageName += "?UniqueId=" + uniqueId;
                    this.navigationArgsMap[PageName] = Args;
                }
                else
                {
                    this.navigationArgsMap.Remove(PageName);
                }
            }
        }

        /// <summary>
        /// Extracts the page name from a route string.
        /// </summary>
        private static bool TryGetPageName(string Route, out string PageName)
        {
            // Initialize as empty string
            PageName = string.Empty;
            if (!string.IsNullOrWhiteSpace(Route))
            {
                string trimmed = Route.TrimStart('.', '/');
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    PageName = trimmed;
                    return true;
                }
            }
            return false;
        }

        private Task<bool> Enqueue(Func<Task> Action)
        {
            TaskCompletionSource<bool> Tcs = new();
            this.taskQueue.Enqueue(async () =>
            {
                try
                {
                    await Action();
                    Tcs.TrySetResult(true);
                }
                catch (Exception Ex)
                {
                    Tcs.TrySetException(Ex);
                }
            });

            if (!this.isExecutingTasks)
            {
                this.isExecutingTasks = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await this.ProcessQueue();
                });
            }

            return Tcs.Task;
        }

        private async Task ProcessQueue()
        {
            try
            {
                while (this.taskQueue.TryDequeue(out Func<Task>? action))
                {
                    await MainThread.InvokeOnMainThreadAsync(action);
                }
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
            finally
            {
                this.isExecutingTasks = false;
            }
        }

        /// <inheritdoc/>
        public ContentPage CurrentPage => this.navigationStack.Count > 0
            ? this.navigationStack.Peek()
            : throw new InvalidOperationException("No pages on navigation stack.");

        /// <inheritdoc/>
        public ContentPage CurrentModalPage => this.modalStack.Count > 0
            ? this.modalStack.Peek()
            : throw new InvalidOperationException("No modal pages on stack.");

        /// <inheritdoc/>
        public Task GoToAsync(string Route)
        {
            return this.GoToAsync<NavigationArgs>(Route, null);
        }

        /// <inheritdoc/>
        public Task GoToAsync<TArgs>(string Route, TArgs? Args)
                where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
                ContentPage? Page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as ContentPage ?? throw new InvalidOperationException($"No page registered for route '{Route}'.");
                NavigationArgs NavigationArgs = Args ?? new();
                NavigationArgs.SetBackArguments(this.navigationArgsStack.Count > 0 ? this.navigationArgsStack.Peek() : null);

                this.latestArguments = NavigationArgs;
                this.navigationArgsStack.Push(NavigationArgs);
                this.navigationStack.Push(Page);
                // Store navigation arguments for this route
                this.PushArgs(Route, NavigationArgs);

                #pragma warning disable 618,612
                if (Application.Current.MainPage is CustomShell customShell)
                    await customShell.SetPageAsync(Page);
                else
                    Application.Current.MainPage = Page;
                #pragma warning restore 618,612
            });
        }

        /// <inheritdoc/>
        public Task GoToAsync(ContentPage Page)
        {
            return this.Enqueue(async () =>
            {
                this.latestArguments = null;
                this.navigationArgsStack.Push(null);
                this.navigationStack.Push(Page);

                var window = Application.Current.Windows.FirstOrDefault();
                if (window?.Page is CustomShell customShell)
                {
                    await customShell.SetPageAsync(Page);
                }
                else if (window != null)
                {
                    window.Page = Page;
                }
            });
        }

        /// <inheritdoc/>
        public Task GoBackAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.navigationStack.Count > 1)
                {
                    this.navigationStack.Pop();
                    this.navigationArgsStack.Pop();
                    ContentPage previous = this.navigationStack.Peek();

                    var window = Application.Current.Windows.FirstOrDefault();
                    if (window?.Page is CustomShell customShell)
                    {
                        await customShell.SetPageAsync(previous);
                    }
                    else if (window != null)
                    {
                        window.Page = previous;
                    }
                }
            });
        }

        /// <inheritdoc/>
        public Task PushModalAsync(string Route)
        {
            return this.Enqueue(async () =>
            {
                ContentPage? page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as ContentPage;
                if (page is null)
                    throw new InvalidOperationException($"No page registered for route '{Route}'.");

                this.modalStack.Push(page);

                var window = Application.Current.Windows.FirstOrDefault();
                if (window?.Page is CustomShell customShell)
                {
                    await customShell.PushModalAsync(page);
                }
                else if (window?.Page != null)
                {
                    await window.Page.Navigation.PushModalAsync(page);
                }
            });
        }

        /// <inheritdoc/>
        public async Task PushModalAsync<TPage>() where TPage : ContentPage
        {
            TPage Page = ServiceRef.Provider.GetRequiredService<TPage>();
            await this.Enqueue(async () =>
            {
                this.modalStack.Push(Page);
                var window = Application.Current.Windows.FirstOrDefault();
                if (window?.Page is CustomShell customShell)
                {
                    await customShell.PushModalAsync(Page);
                }
                else if (window?.Page != null)
                {
                    await window.Page.Navigation.PushModalAsync(Page);
                }
            });

            if (Page.BindingContext is BaseModalViewModel Vm)
                await Vm.Popped;
        }

        /// <inheritdoc/>
        public async Task PushModalAsync<TPage, TViewModel>()
            where TPage : ContentPage
            where TViewModel : BaseModalViewModel
        {
            TPage Page = ServiceRef.Provider.GetRequiredService<TPage>();
            TViewModel ViewModel = ServiceRef.Provider.GetRequiredService<TViewModel>();
            Page.BindingContext = ViewModel;
            await this.Enqueue(async () =>
            {
                this.modalStack.Push(Page);
                var window = Application.Current.Windows.FirstOrDefault();
                if (window?.Page is CustomShell customShell)
                {
                    await customShell.PushModalAsync(Page);
                }
                else if (window?.Page != null)
                {
                    await window.Page.Navigation.PushModalAsync(Page);
                }
            });

            await ViewModel.Popped;
        }

        /// <inheritdoc/>
        public async Task<TReturn?> PushModalAsync<TPage, TViewModel, TReturn>()
            where TPage : ContentPage
            where TViewModel : ReturningModalViewModel<TReturn>
        {
            TPage Page = ServiceRef.Provider.GetRequiredService<TPage>();
            TViewModel ViewModel = ServiceRef.Provider.GetRequiredService<TViewModel>();
            Page.BindingContext = ViewModel;
            await this.Enqueue(async () =>
            {
                this.modalStack.Push(Page);
                var window = Application.Current.Windows.FirstOrDefault();
                if (window?.Page is CustomShell customShell)
                {
                    await customShell.PushModalAsync(Page);
                }
                else if (window?.Page != null)
                {
                    await window.Page.Navigation.PushModalAsync(Page);
                }
            });

            return await ViewModel.Result;
        }

        /// <inheritdoc/>
        public Task PopModalAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.modalStack.Count == 0)
                    return;

                this.modalStack.Pop();

                var window = Application.Current.Windows.FirstOrDefault();
                if (window?.Page is CustomShell customShell)
                {
                    await customShell.PopModalAsync();
                }
                else if (window?.Page != null)
                {
                    await window.Page.Navigation.PopModalAsync();
                }
            });
        }

        /// <inheritdoc/>
        public TArgs? PopLatestArgs<TArgs>() where TArgs : NavigationArgs, new()
        {
            // First, return the most recent arguments
            if (this.latestArguments is TArgs args)
            {
                this.latestArguments = null;
                return args;
            }
            // Next, try to get args by route mapping
            if (this.CurrentPage is ContentPage page)
            {
                string route = Routing.GetRoute(page) ?? string.Empty;
                if (TryGetPageName(route, out string pageName) &&
                    this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs navArgs))
                {
                    this.navigationArgsMap.Remove(pageName);
                    if (navArgs is TArgs typed)
                        return typed;
                }
            }
            return null;
        }
    }
}
