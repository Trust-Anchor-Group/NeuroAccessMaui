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
            return isBack ? TransitionType.Fade : TransitionType.Fade;
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
                NavigationArgs? ParentArgs = this.navigationArgsStack.Count > 0 ? this.navigationArgsStack.Peek() : null;
                NavigationArgs NavArgs = Args ?? new();
                NavArgs.SetBackArguments(ParentArgs, BackMethod, UniqueId);
                this.latestArguments = NavArgs;
                this.PushArgs(Route, NavArgs);

                BaseContentPage screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");

                await EnsureInitializedAsync(screen);

                this.navigationArgsStack.Push(NavArgs);
                this.screenStack.Push(screen);

                try
                {
                    this.isNavigating = true;
                    ServiceRef.LogService.LogDebug($"Navigate request: {Route}");
                    await this.Presenter.ShowScreen(screen, this.GetTransitionType(false)); // SwipeLeft for forward
                    this.Presenter.UpdateBars(screen);
                    // Defer heavy appearing logic so transition frame can paint sooner.
                    _ = screen.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await Task.Yield(); // allow first frame render
                            await screen.OnAppearingAsync();
                            ServiceRef.LogService.LogDebug($"Appearing complete: {Route}");
                        }
                        catch (Exception ex2)
                        {
                            ex2 = Waher.Events.Log.UnnestException(ex2);
                            ServiceRef.LogService.LogException(ex2);
                        }
                        finally
                        {
                            NavArgs.NavigationCompletionSource.TrySetResult(true);
                        }
                    });
                    ServiceRef.LogService.LogDebug($"Screen shown (transition started): {Route}");
                }
                catch (Exception ex)
                {
                    ex = Waher.Events.Log.UnnestException(ex);
                    ServiceRef.LogService.LogException(ex);
                    NavArgs.NavigationCompletionSource.TrySetResult(false);
                }
                finally
                {
                    this.isNavigating = false;
                }
            });
        }

        /// <inheritdoc/>
        public async Task PushModalAsync<TPage>() where TPage : BaseContentPage
        {
            TPage Page = ServiceRef.Provider.GetRequiredService<TPage>();
            await this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(Page);
                this.modalScreenStack.Push(Page);
                await Page.OnAppearingAsync();
                await this.Presenter.ShowModal(Page, TransitionType.Fade);
            });
        }

        /// <inheritdoc/>
        public async Task PushModalAsync<TPage, TViewModel>() where TPage : BaseContentPage where TViewModel : BaseViewModel
        {
            TPage Page = ServiceRef.Provider.GetRequiredService<TPage>();
            TViewModel Vm = ServiceRef.Provider.GetRequiredService<TViewModel>();
            Page.BindingContext = Vm;
            await this.Enqueue(async () =>
            {
                await EnsureInitializedAsync(Page);
                this.modalScreenStack.Push(Page);
                await Page.OnAppearingAsync();
                await this.Presenter.ShowModal(Page, TransitionType.Fade);
            });
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
                    ServiceRef.LogService.LogDebug($"Navigate request (instance): {Page.GetType().Name}");
                    await this.Presenter.ShowScreen(Page, TransitionType.Fade);
                    this.Presenter.UpdateBars(Page);
                    _ = Page.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await Task.Yield();
                            await Page.OnAppearingAsync();
                            ServiceRef.LogService.LogDebug($"Appearing complete (instance): {Page.GetType().Name}");
                        }
                        catch (Exception ex2)
                        {
                            ex2 = Waher.Events.Log.UnnestException(ex2);
                            ServiceRef.LogService.LogException(ex2);
                        }
                    });
                    ServiceRef.LogService.LogDebug($"Screen shown (instance): {Page.GetType().Name}");
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
                BaseContentPage Screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");
                await EnsureInitializedAsync(Screen);
                this.modalScreenStack.Push(Screen);
                await Screen.OnAppearingAsync();
                await this.Presenter.ShowModal(Screen, TransitionType.Fade);
            });
        }

        /// <summary>
        /// Replaces the entire navigation stack with the page registered for the given route.
        /// After completion, back navigation cannot return to previous pages.
        /// </summary>
        /// <param name="Route">Route whose page gets set as new root.</param>
        public Task SetRootAsync(string Route)
        {
            return this.Enqueue(async () =>
            {
                BaseContentPage screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");
                await this.SetRootInternalAsync(screen);
                ServiceRef.LogService.LogDebug($"Root page set to {Route}");
            });
        }

        /// <summary>
        /// Replaces the entire navigation stack with the provided page instance.
        /// After completion, back navigation cannot return to previous pages.
        /// </summary>
        /// <param name="Page">Page instance to set as new root.</param>
        public Task SetRootAsync(BaseContentPage Page)
        {
            return this.Enqueue(async () =>
            {
                await this.SetRootInternalAsync(Page);
                ServiceRef.LogService.LogDebug($"Root page set to instance of {Page.GetType().Name}");
            });
        }

        /// <summary>
        /// Replaces the entire navigation stack with the page registered for the given route.
        /// After completion, back navigation cannot return to previous pages.
        /// </summary>
        /// <param name="Route">Route whose page gets set as new root.</param>
        public Task SetRootAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
                TArgs navArgs = Args ?? new TArgs();
                this.latestArguments = navArgs; // Allow retrieval by root page constructor
                BaseContentPage screen = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or it is not a BaseContentPage.");
                await this.SetRootInternalAsync(screen, navArgs);
                ServiceRef.LogService.LogDebug($"Root page set to {Route}");
            });
        }

        /// <summary>
    /// Internal root setting logic: disposes existing stack &amp; modals, clears argument state, then shows the new root page.
        /// </summary>
        /// <param name="Page">Page to become root.</param>
        private async Task SetRootInternalAsync(BaseContentPage Page, NavigationArgs? Args = null)
        {
            await EnsureInitializedAsync(Page);

            // Dispose modal stack first
            while (this.modalScreenStack.Count > 0)
            {
                BaseContentPage modal = this.modalScreenStack.Pop();
                try
                {
                    await modal.OnDisappearingAsync();
                    await modal.OnDisposeAsync();
                    if (modal is IAsyncDisposable mad) await mad.DisposeAsync();
                    else if (modal is IDisposable md) md.Dispose();
                }
                catch (Exception ex)
                {
                    ServiceRef.LogService.LogException(ex);
                }
            }
            try { await this.Presenter.HideTopModal(); } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }

            // Dispose existing pages in navigation stack
            while (this.screenStack.Count > 0)
            {
                BaseContentPage existing = this.screenStack.Pop();
                try
                {
                    await existing.OnDisappearingAsync();
                    await existing.OnDisposeAsync();
                    if (existing is IAsyncDisposable ad) await ad.DisposeAsync();
                    else if (existing is IDisposable d) d.Dispose();
                    this.RemoveArgsFor(existing);
                }
                catch (Exception ex)
                {
                    ServiceRef.LogService.LogException(ex);
                }
            }

            // Clear navigation args & stacks
            this.navigationArgsStack.Clear();
            this.navigationArgsMap.Clear();
            this.latestArguments = Args; // Stored for constructor retrieval

            // Push new root (no args -> cannot go back beyond it)
            this.navigationArgsStack.Push(null);
            this.screenStack.Push(Page);
            if (Args is not null)
            {
                // Store for lookup using page name
                string route = Routing.GetRoute(Page) ?? Page.GetType().Name;
                this.PushArgs(route, Args);
            }

            try
            {
                this.isNavigating = true;
                ServiceRef.LogService.LogDebug($"Root navigate request: {Page.GetType().Name}");
                await this.Presenter.ShowScreen(Page, TransitionType.Fade);
                this.Presenter.UpdateBars(Page);
                _ = Page.Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        await Task.Yield();
                        await Page.OnAppearingAsync();
                        ServiceRef.LogService.LogDebug($"Root appearing complete: {Page.GetType().Name}");
                    }
                    catch (Exception ex2)
                    {
                        ServiceRef.LogService.LogException(ex2);
                    }
                    finally
                    {
                        Args?.NavigationCompletionSource.TrySetResult(true);
                    }
                });
                ServiceRef.LogService.LogDebug($"Root screen shown: {Page.GetType().Name}");
            }
            catch (Exception ex)
            {
                ServiceRef.LogService.LogException(ex);
            }
            finally
            {
                this.isNavigating = false;
            }
        }

        /// <summary>
        /// Pops all pages until only the root page remains on the navigation stack.
        /// </summary>
        public Task PopToRootAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.screenStack.Count <= 1)
                {
                    ServiceRef.LogService.LogDebug("PopToRoot: Already at root.");
                    return;
                }

                this.isNavigating = true;
                try
                {
                    // Dispose everything above root
                    while (this.screenStack.Count > 1)
                    {
                        NavigationArgs? poppedArgs = this.navigationArgsStack.Pop();
                        BaseContentPage popped = this.screenStack.Pop();
                        try
                        {
                            await popped.OnDisappearingAsync();
                            if (popped is IAsyncDisposable asyncDisposable)
                                await asyncDisposable.DisposeAsync();
                            else if (popped is IDisposable disposable)
                                disposable.Dispose();
                            this.RemoveArgsFor(popped);
                        }
                        catch (Exception ex)
                        {
                            ServiceRef.LogService.LogException(ex);
                        }
                    }

                    // Show root again with back transition (optional fade)
                    BaseContentPage root = this.screenStack.Peek();
                    await this.Presenter.ShowScreen(root, TransitionType.Fade);
                    _ = root.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await Task.Yield();
                            await root.OnAppearingAsync();
                            ServiceRef.LogService.LogDebug("PopToRoot: Root appearing complete.");
                        }
                        catch (Exception ex2)
                        {
                            ServiceRef.LogService.LogException(ex2);
                        }
                    });
                    this.Presenter.UpdateBars(root);
                    ServiceRef.LogService.LogDebug("PopToRoot: Navigation stack reset to root.");
                }
                finally
                {
                    this.isNavigating = false;
                }
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
                await this.Presenter.ShowScreen(Target, this.GetTransitionType(true)); // SwipeRight for back
                _ = Target.Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        await Task.Yield();
                        await Target.OnAppearingAsync();
                        ServiceRef.LogService.LogDebug("Back: Target appearing complete.");
                    }
                    catch (Exception ex2)
                    {
                        ServiceRef.LogService.LogException(ex2);
                    }
                });
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
