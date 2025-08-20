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
        // Determines transition type for navigation direction
        private TransitionType GetTransitionType(bool isBack)
        {
            return isBack ? TransitionType.SwipeRight : TransitionType.SwipeLeft;
        }
        private readonly Stack<BaseContentPage> modalScreenStack = new();
        private readonly Stack<NavigationArgs?> navigationArgsStack = new();
        private bool isNavigating = false; // Mirrors UiService behavior

        private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
        private bool isExecutingTasks;
        private NavigationArgs? latestArguments;
        private readonly Dictionary<string, NavigationArgs> navigationArgsMap = new();
        private bool IsQueueBusy => this.isExecutingTasks || this.isNavigating;

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

        // Generic enqueue supporting a return value.
        private Task<TResult> Enqueue<TResult>(Func<Task<TResult>> Action)
        {
            TaskCompletionSource<TResult> Tcs = new();
            this.taskQueue.Enqueue(async () =>
            {
                try
                {
                    TResult Result = await Action();
                    Tcs.TrySetResult(Result);
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
        // Computes number of ".." segments in a back route string.
        private static int ComputeBackLevels(NavigationArgs? args)
        {
            if (args is null)
                return 1;

            try
            {
                string backRoute = args.GetBackRoute();
                if (string.IsNullOrWhiteSpace(backRoute))
                    return 1;

                return backRoute.Split('/', StringSplitOptions.RemoveEmptyEntries).Count(s => s == "..");
            }
            catch
            {
                return 1;
            }
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
        // Interface method (single parameter)
        public Task GoToAsync(string Route) => this.GoToAsync(Route, BackMethod.Inherited, null);

        // Extended overload with BackMethod & UniqueId
        public Task GoToAsync(string Route, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null)
            => this.GoToAsync<NavigationArgs>(Route, null, BackMethod, UniqueId);

        /// <inheritdoc/>
        // Interface method (two parameters)
        public Task GoToAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new()
            => this.GoToAsync(Route, Args, BackMethod.Inherited, null);

        // Extended overload with BackMethod & UniqueId
        public Task GoToAsync<TArgs>(string Route, TArgs? Args, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null) where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
                NavigationArgs? parentArgs = this.navigationArgsStack.Count > 0 ? this.navigationArgsStack.Peek() : null;
                NavigationArgs navArgs = Args ?? new();
                navArgs.SetBackArguments(parentArgs, BackMethod, UniqueId);
                this.latestArguments = navArgs;
                this.PushArgs(Route, navArgs);

                BaseContentPage screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");

                await EnsureInitializedAsync(screen);

                this.navigationArgsStack.Push(navArgs);
                this.screenStack.Push(screen);

                try
                {
                    this.isNavigating = true;
                    await this.Presenter.ShowScreen(screen, GetTransitionType(false)); // SwipeLeft for forward
                    this.Presenter.UpdateBars(screen);
                    await screen.OnAppearingAsync();
                    ServiceRef.LogService.LogDebug($"Navigated to {Route}");
                }
                catch (Exception ex)
                {
                    ex = Waher.Events.Log.UnnestException(ex);
                    ServiceRef.LogService.LogException(ex);
                }
                finally
                {
                    this.isNavigating = false;
                    navArgs.NavigationCompletionSource.TrySetResult(true);
                }
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
            if (this.latestArguments is TArgs la)
            {
                this.latestArguments = null; // allow single retrieval only
                return la;
            }
            if (this.screenStack.Count > 0)
            {
                BaseContentPage page = this.screenStack.Peek();
                string route = Routing.GetRoute(page) ?? string.Empty;
                if (TryGetPageName(route, out string pageName) && this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs navArgs))
                {
                    this.navigationArgsMap.Remove(pageName);
                    if (navArgs is TArgs typed) return typed; // do not null latestArguments here (already not matching)
                }
            }
            return null;
        }

        // Removed Page support: all views are BaseContentPage.

        /// <summary>
        /// Navigate to a page instance already constructed.
        /// </summary>
        public Task GoToAsync(BaseContentPage Page)
        {
            return this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(Page);
                this.latestArguments = null;
                this.navigationArgsStack.Push(null);
                this.screenStack.Push(Page);
                try
                {
                    this.isNavigating = true;
                    await this.Presenter.ShowScreen(Page, TransitionType.Fade);
                    this.Presenter.UpdateBars(Page);
                    await Page.OnAppearingAsync();
                }
                finally
                {
                    this.isNavigating = false;
                }
            });
        }

        /// <summary>
        /// Goes back according to current navigation arguments (multi-level) or pops one page.
        /// </summary>
        public Task GoBackAsync() => this.Enqueue(async () =>
        {
            await this.InternalGoBackAsync();
        });

        /// <summary>
        /// Push modal by route.
        /// </summary>
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
        #region Back Handling

        public bool WouldHandleBack()
        {
            /*
            if (this.IsQueueBusy)
                return true;
            if (this.modalScreenStack.Count > 0)
                return true;
            if (this.screenStack.Count > 1)
                return true;
            if (this.screenStack.Count > 0)
            {
                BaseContentPage page = this.screenStack.Peek();
                if (page is IBackButtonHandler || page.BindingContext is IBackButtonHandler)
                    return true;
            }
            return false;*/
            //Always Handle back for simplicity
            return true;
        }

        public async Task<bool> HandleBackAsync()
        {
            if (this.IsQueueBusy)
            {
                ServiceRef.LogService.LogDebug("Back: Ignored (busy)");
                return true;
            }

            return await this.Enqueue(async () =>
            {
                if (this.modalScreenStack.Count > 0)
                {
                    ServiceRef.LogService.LogDebug("Back: Pop modal");
                    await this.InternalPopModalAsync();
                    return true;
                }

                BaseContentPage? page = this.screenStack.Count > 0 ? this.screenStack.Peek() : null;

                if (page is IBackButtonHandler pageHandler)
                {
                    try
                    {
                        if (await pageHandler.OnBackButtonPressedAsync())
                            return true;
                    }
                    catch (Exception ex)
                    {
                        ServiceRef.LogService.LogException(ex);
                    }
                }

                if (page?.BindingContext is IBackButtonHandler vmHandler)
                {
                    try
                    {
                        if (await vmHandler.OnBackButtonPressedAsync())
                            return true;
                    }
                    catch (Exception ex)
                    {
                        ServiceRef.LogService.LogException(ex);
                    }
                }

                if (this.screenStack.Count > 1)
                {
                    ServiceRef.LogService.LogDebug("Back: Pop page(s)");
                    await this.InternalGoBackAsync();
                    return true;
                }

                ServiceRef.LogService.LogDebug("Back: Not handled (ignore)");
                return true;
            });
        }

        private async Task InternalPopModalAsync()
        {
            if (this.modalScreenStack.Count == 0)
                return;
            BaseContentPage top = this.modalScreenStack.Pop();
            await top.OnDisappearingAsync();
            await top.OnDisposeAsync();
            if (top is IAsyncDisposable iad2) await iad2.DisposeAsync();
            else if (top is IDisposable d2) d2.Dispose();
            await this.Presenter.HideTopModal();
        }

        private async Task InternalGoBackAsync()
        {
            if (this.screenStack.Count <= 1)
                return;
            this.isNavigating = true;
            try
            {
                NavigationArgs? PoppedArgs = this.navigationArgsStack.Pop();
                BaseContentPage Popped = this.screenStack.Pop();
                await Popped.OnDisappearingAsync();
                if (Popped is IAsyncDisposable Disposable)
                    await Disposable.DisposeAsync();
                else if (Popped is IDisposable Disposable2)
                    Disposable2.Dispose();
                this.RemoveArgsFor(Popped);

                int Levels = ComputeBackLevels(PoppedArgs);
                for (int i = 1; i < Levels && this.screenStack.Count > 1; i++)
                {
                    NavigationArgs? ExtraArgs = this.navigationArgsStack.Pop();
                    BaseContentPage Extra = this.screenStack.Pop();
                    await Extra.OnDisappearingAsync();
                    if (Extra is IAsyncDisposable ExtraAsync)
                        await ExtraAsync.DisposeAsync();
                    else if (Extra is IDisposable ExtraDisposable)
                        ExtraDisposable.Dispose();
                    this.RemoveArgsFor(Extra);
                }

                BaseContentPage Target = this.screenStack.Peek();
                await this.Presenter.ShowScreen(Target, GetTransitionType(true)); // SwipeRight for back
                await Target.OnAppearingAsync();
                this.Presenter.UpdateBars(Target);
            }
            finally
            {
                this.isNavigating = false;
            }
        }
        #endregion
    }
}
