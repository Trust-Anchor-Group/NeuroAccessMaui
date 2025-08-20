using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages;  // for ILifeCycleView
using Waher.Runtime.Inventory;
using Waher.Script.Functions.ComplexNumbers;

#pragma warning disable CS0618, CS8600, CS8602, CS8603, CS8604, CS8765

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Default implementation of <see cref="INavigationService"/> that uses MAUI Routing for route-based page instantiation.
    /// </summary>
    [Singleton]
    public class NavigationService : INavigationService
    {
        // Screen & modal stacks (all are BaseContentPage now)
        private readonly Stack<BaseContentPage> screenStack = new();
        private readonly Stack<BaseContentPage> modalScreenStack = new();
        private readonly Stack<NavigationArgs?> navigationArgsStack = new();

        private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
        private bool isExecutingTasks;
        private NavigationArgs? latestArguments;
        private readonly Dictionary<string, NavigationArgs> navigationArgsMap = new();

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
                MainThread.BeginInvokeOnMainThread(async () => await this.ProcessQueue());
            }

            return Tcs.Task;
        }

        private async Task ProcessQueue()
        {
            try
            {
                while (this.taskQueue.TryDequeue(out Func<Task>? action))
                {
                    await action(); // Already on main thread
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
        public BaseContentPage CurrentPage => this.screenStack.Count > 0
            ? this.screenStack.Peek()
            : throw new InvalidOperationException("No pages on navigation stack.");

        /// <inheritdoc/>
        public BaseContentPage CurrentModalPage => this.modalScreenStack.Count > 0
            ? this.modalScreenStack.Peek()
            : throw new InvalidOperationException("No modal pages on stack.");

        private IShellPresenter Presenter =>
            (Application.Current.MainPage as NavigationPage)?.CurrentPage as IShellPresenter
            ?? Application.Current.MainPage as IShellPresenter
            ?? throw new InvalidOperationException("CustomShell presenter not found.");

        private static async Task EnsureInitializedAsync(object obj)
        {
            if (obj is ILifeCycleView lcv)
            {
                Type type = obj.GetType();
                System.Reflection.PropertyInfo? prop = type.GetProperty("IsInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                bool already = false;
                if (prop?.CanRead == true)
                    already = (bool)(prop.GetValue(obj) ?? false);
                if (!already)
                {
                    await lcv.OnInitializeAsync();
                    if (prop?.CanWrite == true)
                        prop.SetValue(obj, true);
                }
            }
        }
        private static int ComputeBackLevels(NavigationArgs? args)
        {
            if (args is null) return 1;
            try
            {
                string backRoute = args.GetBackRoute();
                if (string.IsNullOrWhiteSpace(backRoute)) return 1;
                return backRoute.Split('/', StringSplitOptions.RemoveEmptyEntries).Count(s => s == "..");
            }
            catch { return 1; }
        }
        private void RemoveArgsFor(object view)
        {
            string route = string.Empty;
            if (view is BindableObject bo)
                route = Routing.GetRoute(bo) ?? string.Empty;
            if (TryGetPageName(route, out string pageName))
            {
                foreach (KeyValuePair<string, NavigationArgs> kv in this.navigationArgsMap.ToList())
                {
                    string key = kv.Key;
                    if (key.StartsWith(pageName, StringComparison.OrdinalIgnoreCase))
                        this.navigationArgsMap.Remove(key);
                }
            }
        }

        /// <inheritdoc/>
        public Task GoToAsync(string Route)
        {
            return this.GoToAsync<NavigationArgs>(Route, null);
        }

        /// <inheritdoc/>
        public Task GoToAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
                BaseContentPage screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");
                await EnsureInitializedAsync(screen);
                NavigationArgs navArgs = Args ?? new();
                navArgs.SetBackArguments(this.navigationArgsStack.Count > 0 ? this.navigationArgsStack.Peek() : null);
                this.latestArguments = navArgs;
                this.navigationArgsStack.Push(navArgs);
                this.screenStack.Push(screen);
                this.PushArgs(Route, navArgs);
                await screen.OnAppearingAsync();
                await this.Presenter.ShowScreen(screen, TransitionType.Fade);
                this.Presenter.UpdateBars(screen);
                ServiceRef.LogService.LogDebug($"Navigating to {Route}");
            });
        }

        /// <inheritdoc/>
        public Task GoToAsync(BaseContentPage Page)
        {
            return this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(Page);
                this.latestArguments = null;
                this.navigationArgsStack.Push(null);
                this.screenStack.Push(Page);
                await Page.OnAppearingAsync();
                await this.Presenter.ShowScreen(Page, TransitionType.Fade);
                this.Presenter.UpdateBars(Page);
            });
        }

        /// <inheritdoc/>
        public Task GoBackAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.screenStack.Count > 1)
                {
                    NavigationArgs? poppedArgs = this.navigationArgsStack.Pop();
                    BaseContentPage popped = this.screenStack.Pop();
                    await popped.OnDisappearingAsync();
                    if (popped is IAsyncDisposable iad2) await iad2.DisposeAsync();
                    else if (popped is IDisposable d2) d2.Dispose();
                    this.RemoveArgsFor(popped);
                    int levels = ComputeBackLevels(poppedArgs);
                    for (int i = 1; i < levels && this.screenStack.Count > 1; i++)
                    {
                        this.navigationArgsStack.Pop();
                        BaseContentPage extra = this.screenStack.Pop();
                        await extra.OnDisappearingAsync();
                        if (extra is IAsyncDisposable extraAsync) await extraAsync.DisposeAsync();
                        else if (extra is IDisposable extraDisp) extraDisp.Dispose();
                        this.RemoveArgsFor(extra);
                    }
                    BaseContentPage target = this.screenStack.Peek();
                    await this.Presenter.ShowScreen(target);
                    await target.OnAppearingAsync();
                    this.Presenter.UpdateBars(target);
                }
            });
        }

        /// <inheritdoc/>
        public Task PushModalAsync(string Route)
        {
            return this.Enqueue(async () =>
            {
                BaseContentPage screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");
                await EnsureInitializedAsync(screen);
                this.modalScreenStack.Push(screen);
                await screen.OnAppearingAsync();
                await this.Presenter.ShowModal(screen, TransitionType.Fade);
            });
        }

        /// <inheritdoc/>
        public async Task PushModalAsync<TPage>() where TPage : BaseContentPage
        {
            TPage page = ServiceRef.Provider.GetRequiredService<TPage>();
            await this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(page);
                this.modalScreenStack.Push(page);
                await page.OnAppearingAsync();
                await this.Presenter.ShowModal(page, TransitionType.Fade);
            });
            if (page.BindingContext is BaseModalViewModel vm) await vm.Popped;
        }

        /// <inheritdoc/>
        public async Task PushModalAsync<TPage, TViewModel>() where TPage : BaseContentPage where TViewModel : BaseModalViewModel
        {
            TPage page = ServiceRef.Provider.GetRequiredService<TPage>();
            TViewModel vm = ServiceRef.Provider.GetRequiredService<TViewModel>();
            page.BindingContext = vm;
            await this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(page);
                this.modalScreenStack.Push(page);
                await page.OnAppearingAsync();
                await this.Presenter.ShowModal(page, TransitionType.Fade);
            });
            await vm.Popped;
        }

        /// <inheritdoc/>
        public async Task<TReturn?> PushModalAsync<TPage, TViewModel, TReturn>() where TPage : BaseContentPage where TViewModel : ReturningModalViewModel<TReturn>
        {
            TPage page = ServiceRef.Provider.GetRequiredService<TPage>();
            TViewModel vm = ServiceRef.Provider.GetRequiredService<TViewModel>();
            page.BindingContext = vm;
            await this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(page);
                this.modalScreenStack.Push(page);
                await page.OnAppearingAsync();
                await this.Presenter.ShowModal(page, TransitionType.Fade);
            });
            return await vm.Result;
        }

        /// <inheritdoc/>
        public Task PopModalAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.modalScreenStack.Count == 0)
                    return;
                BaseContentPage top = this.modalScreenStack.Pop();
                await top.OnDisappearingAsync();
				await top.OnDisposeAsync();
				if (top is IAsyncDisposable iad2) await iad2.DisposeAsync();
                else if (top is IDisposable d2) d2.Dispose();
                await this.Presenter.HideTopModal();
            });
        }

        /// <inheritdoc/>
        public TArgs? PopLatestArgs<TArgs>() where TArgs : NavigationArgs, new()
        {
            if (this.latestArguments is TArgs la) return la;
            if (this.screenStack.Count > 0)
            {
                BaseContentPage page = this.screenStack.Peek();
                string route = Routing.GetRoute(page) ?? string.Empty;
                if (TryGetPageName(route, out string pageName) && this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs navArgs))
                {
                    this.navigationArgsMap.Remove(pageName);
                    if (navArgs is TArgs typed) return typed;
                }
            }
            return null;
        }

        // Removed Page support: all views are BaseContentPage.
    }
}
