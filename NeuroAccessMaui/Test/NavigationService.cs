using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.UI.Pages;
using Waher.Runtime.Inventory;

#pragma warning disable CS0618, CS8600, CS8602, CS8603, CS8604, CS8765

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Navigation service managing a custom stack of <see cref="BaseContentPage"/> instances.
    /// Modal navigation is deprecated in favor of blocking popups (see <see cref="PopupOptions.IsBlocking"/>).
    /// </summary>
    [Singleton]
    public class NavigationService : INavigationService
    {
        private readonly Stack<BaseContentPage> screenStack = new();
        private readonly Stack<NavigationArgs?> navigationArgsStack = new();
        private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
        private readonly Dictionary<string, NavigationArgs> navigationArgsMap = new();

        private bool isNavigating;
        private bool isExecuting;
        private NavigationArgs? latestArguments;

        private bool IsBusy => this.isNavigating || this.isExecuting;

        private IShellPresenter Presenter =>
            (Application.Current.MainPage as NavigationPage)?.CurrentPage as IShellPresenter
            ?? Application.Current.MainPage as IShellPresenter
            ?? throw new InvalidOperationException("CustomShell presenter not found.");

        #region INavigationService implementation

        /// <inheritdoc/>
        public BaseContentPage CurrentPage => this.screenStack.Count > 0 ? this.screenStack.Peek() : throw new InvalidOperationException("Navigation stack is empty.");

        /// <inheritdoc/>
        [Obsolete("Modal pages removed. Use blocking popups (PopupOptions.IsBlocking) instead.")]
        public BaseContentPage CurrentModalPage => throw new InvalidOperationException("Modal stack removed. Use PopupService.");

        /// <inheritdoc/>
        public Task GoToAsync(string Route) => this.GoToAsync(Route, Services.UI.BackMethod.Inherited, null);

        /// <inheritdoc/>
        public Task GoToAsync(string Route, Services.UI.BackMethod BackMethod, string? UniqueId = null) => this.GoToAsync<NavigationArgs>(Route, null, BackMethod, UniqueId);

        /// <inheritdoc/>
        public Task GoToAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new() => this.GoToAsync(Route, Args, Services.UI.BackMethod.Inherited, null);

        /// <inheritdoc/>
        public Task GoToAsync<TArgs>(string Route, TArgs? Args, Services.UI.BackMethod BackMethod, string? UniqueId = null) where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
                NavigationArgs? parent = this.navigationArgsStack.Count > 0 ? this.navigationArgsStack.Peek() : null;
                NavigationArgs navArgs = Args ?? new TArgs();
                navArgs.SetBackArguments(parent, BackMethod, UniqueId);
                this.latestArguments = navArgs;
                this.PushArgs(Route, navArgs);

                BaseContentPage page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or not a BaseContentPage");

                this.navigationArgsStack.Push(navArgs);
                this.screenStack.Push(page);

                Task initializationTask = Task.CompletedTask;
                try
                {
                    this.isNavigating = true;
                    await this.Presenter.ShowScreen(page, TransitionType.Fade);
                    this.Presenter.UpdateBars(page);
                    initializationTask = EnsureInitializedAsync(page);
                    _ = page.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await initializationTask;
                            await Task.Yield();
                            await page.OnAppearingAsync();
                        }
                        catch (Exception ex) { ServiceRef.LogService.LogException(Waher.Events.Log.UnnestException(ex)); }
                        finally { navArgs.NavigationCompletionSource.TrySetResult(true); }
                    });
                }
                catch (Exception ex)
                {
                    ServiceRef.LogService.LogException(Waher.Events.Log.UnnestException(ex));
                    navArgs.NavigationCompletionSource.TrySetResult(false);
                }
                finally { this.isNavigating = false; }
            });
        }

        /// <inheritdoc/>
        public Task GoToAsync(BaseContentPage Page)
        {
            return this.Enqueue(async () =>
            {
                this.latestArguments = null;
                this.navigationArgsStack.Push(null);
                this.screenStack.Push(Page);
                Task initializationTask = Task.CompletedTask;
                try
                {
                    this.isNavigating = true;
                    await this.Presenter.ShowScreen(Page, TransitionType.Fade);
                    this.Presenter.UpdateBars(Page);
                    initializationTask = EnsureInitializedAsync(Page);
                    _ = Page.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await initializationTask;
                            await Task.Yield();
                            await Page.OnAppearingAsync();
                        }
                        catch (Exception ex) { ServiceRef.LogService.LogException(Waher.Events.Log.UnnestException(ex)); }
                    });
                }
                finally { this.isNavigating = false; }
            });
        }

        /// <inheritdoc/>
        public Task SetRootAsync(string Route)
        {
            return this.Enqueue(async () =>
            {
                BaseContentPage page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or not a BaseContentPage");
                await this.SetRootInternalAsync(page, null);
            });
        }

        /// <inheritdoc/>
        public Task SetRootAsync(BaseContentPage Page) => this.Enqueue(async () => await this.SetRootInternalAsync(Page, null));

        /// <inheritdoc/>
        public Task SetRootAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
                TArgs navArgs = Args ?? new TArgs();
                this.latestArguments = navArgs;
                BaseContentPage page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                    ?? throw new InvalidOperationException($"No page registered for route '{Route}' or not a BaseContentPage");
                await this.SetRootInternalAsync(page, navArgs);
            });
        }

        /// <inheritdoc/>
        public Task GoBackAsync() => this.Enqueue(this.InternalGoBackAsync);

        /// <inheritdoc/>
        public Task PopToRootAsync() => this.Enqueue(async () =>
        {
            if (this.screenStack.Count <= 1)
                return;
            List<BaseContentPage> removedPages = new();
            bool transitionCompleted = false;
            this.isNavigating = true;
            try
            {
                while (this.screenStack.Count > 1)
                {
                    _ = this.navigationArgsStack.Pop();
                    BaseContentPage popped = this.screenStack.Pop();
                    this.RemoveArgsFor(popped);
                    removedPages.Add(popped);
                }
                BaseContentPage root = this.screenStack.Peek();
                await this.Presenter.ShowScreen(root, TransitionType.Fade);
                transitionCompleted = true;
                _ = root.Dispatcher.Dispatch(async () =>
                {
                    try { await Task.Yield(); await root.OnAppearingAsync(); } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
                });
                this.Presenter.UpdateBars(root);
            }
            catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            finally { this.isNavigating = false; }

            await Task.Yield();
            for (int i = 0; i < removedPages.Count; i++)
            {
                BaseContentPage removed = removedPages[i];
                try
                {
                    bool skipDisappearing = transitionCompleted && i == 0;
                    if (!skipDisappearing)
                        await removed.OnDisappearingAsync();
                    await removed.OnDisposeAsync();
                    if (removed is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else if (removed is IDisposable disposable)
                        disposable.Dispose();
                }
                catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            }
        });

        /// <inheritdoc/>
        [Obsolete("Use IPopupService.PushAsync with PopupOptions.IsBlocking instead.")]
        public Task PushModalAsync(string Route) => this.Enqueue(async () =>
        {
            BaseContentPage page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as BaseContentPage
                ?? throw new InvalidOperationException($"No page registered for route '{Route}' or not a BaseContentPage");
            await EnsureInitializedAsync(page);
            await ServiceRef.PopupService.PushAsync(page, PopupOptions.CreateModal());
            await page.OnAppearingAsync();
        });

        /// <inheritdoc/>
        [Obsolete("Use IPopupService.PushAsync with PopupOptions.IsBlocking instead.")]
        public Task PushModalAsync<TPage>() where TPage : BaseContentPage => this.Enqueue(async () =>
        {
            TPage page = ServiceRef.Provider.GetRequiredService<TPage>();
            await EnsureInitializedAsync(page);
            await ServiceRef.PopupService.PushAsync(page, PopupOptions.CreateModal());
            await page.OnAppearingAsync();
        });

        /// <inheritdoc/>
        [Obsolete("Use IPopupService.PushAsync with PopupOptions.IsBlocking instead.")]
        public Task PushModalAsync<TPage, TViewModel>() where TPage : BaseContentPage where TViewModel : BaseViewModel => this.Enqueue(async () =>
        {
            TPage page = ServiceRef.Provider.GetRequiredService<TPage>();
            TViewModel vm = ServiceRef.Provider.GetRequiredService<TViewModel>();
            page.BindingContext = vm;
            await EnsureInitializedAsync(page);
            await ServiceRef.PopupService.PushAsync(page, PopupOptions.CreateModal());
            await page.OnAppearingAsync();
        });

        /// <inheritdoc/>
        [Obsolete("Use IPopupService.PopAsync instead.")]
        public Task PopModalAsync() => this.Enqueue(async () => await ServiceRef.PopupService.PopAsync());

        /// <inheritdoc/>
        public TArgs? PopLatestArgs<TArgs>() where TArgs : NavigationArgs, new()
        {
            if (this.latestArguments is TArgs match)
            {
                this.latestArguments = null;
                return match;
            }
            if (this.screenStack.Count > 0)
            {
                BaseContentPage page = this.screenStack.Peek();
                string route = Routing.GetRoute(page) ?? string.Empty;
                if (TryGetPageName(route, out string pageName))
                {
                    foreach (KeyValuePair<string, NavigationArgs> kv in this.navigationArgsMap.ToList())
                    {
                        if (kv.Key.StartsWith(pageName, StringComparison.OrdinalIgnoreCase))
                        {
                            this.navigationArgsMap.Remove(kv.Key);
                            if (kv.Value is TArgs typed)
                                return typed;
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region Back handling

        public bool WouldHandleBack() => true; // Always intercept

        public Task<bool> HandleBackAsync()
        {
            if (this.IsBusy)
                return Task.FromResult(true);
            TaskCompletionSource<bool> tcs = new();
            _ = this.Enqueue(async () =>
            {
                try
                {
                    if (ServiceRef.PopupService.HasOpenPopups)
                    {
                        // PopupService handles dismissal via shell event; just consume.
                        tcs.TrySetResult(true);
                        return;
                    }
                    BaseContentPage? page = this.screenStack.Count > 0 ? this.screenStack.Peek() : null;
                    if (page is IBackButtonHandler pageHandler)
                    {
                        try { if (await pageHandler.OnBackButtonPressedAsync()) { tcs.TrySetResult(true); return; } } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
                    }
                    if (page?.BindingContext is IBackButtonHandler vmHandler)
                    {
                        try { if (await vmHandler.OnBackButtonPressedAsync()) { tcs.TrySetResult(true); return; } } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
                    }
                    if (this.screenStack.Count > 1)
                    {
                        await this.InternalGoBackAsync();
                        tcs.TrySetResult(true);
                        return;
                    }
                    tcs.TrySetResult(true); // consume
                }
                catch (Exception ex)
                {
                    ServiceRef.LogService.LogException(ex);
                    tcs.TrySetResult(true);
                }
            });
            return tcs.Task;
        }

        private async Task InternalGoBackAsync()
        {
            if (this.screenStack.Count <= 1)
                return;
            List<BaseContentPage> removedPages = new();
            bool transitionCompleted = false;
            try
            {
                NavigationArgs? poppedArgs = this.navigationArgsStack.Pop();
                BaseContentPage popped = this.screenStack.Pop();
                this.RemoveArgsFor(popped);
                removedPages.Add(popped);

                int levels = ComputeBackLevels(poppedArgs);
                for (int i = 1; i < levels && this.screenStack.Count > 1; i++)
                {
                    _ = this.navigationArgsStack.Pop();
                    BaseContentPage extra = this.screenStack.Pop();
                    this.RemoveArgsFor(extra);
                    removedPages.Add(extra);
                }

                BaseContentPage target = this.screenStack.Peek();
                this.isNavigating = true;
                await this.Presenter.ShowScreen(target, TransitionType.Fade);
                transitionCompleted = true;
                _ = target.Dispatcher.Dispatch(async () =>
                {
                    try { await Task.Yield(); await target.OnAppearingAsync(); } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
                });
                this.Presenter.UpdateBars(target);
            }
            catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            finally { this.isNavigating = false; }

            await Task.Yield();
            for (int i = 0; i < removedPages.Count; i++)
            {
                BaseContentPage removed = removedPages[i];
                try
                {
                    bool skipDisappearing = transitionCompleted && i == 0;
                    if (!skipDisappearing)
                        await removed.OnDisappearingAsync();
                    await removed.OnDisposeAsync();
                    if (removed is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else if (removed is IDisposable disposable)
                        disposable.Dispose();
                }
                catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            }
        }

        #endregion

        #region Queue & helpers

        private Task Enqueue(Func<Task> action)
        {
            TaskCompletionSource<bool> tcs = new();
            this.taskQueue.Enqueue(async () =>
            {
                try { await action(); tcs.TrySetResult(true); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            });
            if (!this.isExecuting)
            {
                this.isExecuting = true;
                MainThread.BeginInvokeOnMainThread(async () => await this.ProcessQueueAsync());
            }
            return tcs.Task;
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (this.taskQueue.TryDequeue(out Func<Task>? action))
                    await action();
            }
            catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            finally { this.isExecuting = false; }
        }

        private static async Task EnsureInitializedAsync(object obj)
        {
            if (obj is ILifeCycleView lcv)
            {
                System.Reflection.PropertyInfo? prop = obj.GetType().GetProperty("IsInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                bool isInit = false;
                if (prop?.CanRead == true)
                    isInit = (bool)(prop.GetValue(obj) ?? false);
                if (!isInit)
                {
                    await lcv.OnInitializeAsync();
                    if (prop?.CanWrite == true)
                        prop.SetValue(obj, true);
                }
            }
        }

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
            catch { return 1; }
        }

        private void PushArgs(string Route, NavigationArgs Args)
        {
            this.latestArguments = Args;
            if (TryGetPageName(Route, out string pageName))
            {
                string key = pageName;
                if (!string.IsNullOrEmpty(Args.UniqueId))
                    key += "?UniqueId=" + Args.UniqueId;
                this.navigationArgsMap[key] = Args;
            }
        }

        private void RemoveArgsFor(object view)
        {
            string route = string.Empty;
            if (view is BindableObject bindable)
                route = Routing.GetRoute(bindable) ?? string.Empty;
            if (TryGetPageName(route, out string pageName))
            {
                foreach (KeyValuePair<string, NavigationArgs> kv in this.navigationArgsMap.ToList())
                {
                    if (kv.Key.StartsWith(pageName, StringComparison.OrdinalIgnoreCase))
                        this.navigationArgsMap.Remove(kv.Key);
                }
            }
        }

        private static bool TryGetPageName(string Route, out string PageName)
        {
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

        private async Task SetRootInternalAsync(BaseContentPage Page, NavigationArgs? Args)
        {
            List<BaseContentPage> removedPages = new();
            while (this.screenStack.Count > 0)
            {
                BaseContentPage existing = this.screenStack.Pop();
                removedPages.Add(existing);
                this.RemoveArgsFor(existing);
            }
            this.navigationArgsStack.Clear();
            this.navigationArgsMap.Clear();
            this.latestArguments = Args;
            this.navigationArgsStack.Push(null);
            this.screenStack.Push(Page);
            if (Args is not null)
            {
                string route = Routing.GetRoute(Page) ?? Page.GetType().Name;
                this.PushArgs(route, Args);
            }
            Task initializationTask = Task.CompletedTask;
            bool transitionCompleted = false;
            try
            {
                this.isNavigating = true;
                await this.Presenter.ShowScreen(Page, TransitionType.Fade);
                transitionCompleted = true;
                this.Presenter.UpdateBars(Page);
                initializationTask = EnsureInitializedAsync(Page);
                _ = Page.Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        await initializationTask;
                        await Task.Yield();
                        await Page.OnAppearingAsync();
                    }
                    catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
                    finally { Args?.NavigationCompletionSource.TrySetResult(true); }
                });
            }
            catch (Exception ex)
            {
                ServiceRef.LogService.LogException(ex);
                Args?.NavigationCompletionSource.TrySetResult(false);
            }
            finally { this.isNavigating = false; }

            await Task.Yield();
            BaseContentPage? outgoingPage = removedPages.Count > 0 ? removedPages[0] : null;
            foreach (BaseContentPage removed in removedPages)
            {
                try
                {
                    bool skipDisappearing = transitionCompleted && ReferenceEquals(removed, outgoingPage);
                    if (!skipDisappearing)
                        await removed.OnDisappearingAsync();
                    await removed.OnDisposeAsync();
                    if (removed is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else if (removed is IDisposable disposable)
                        disposable.Dispose();
                }
                catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            }
        }

        #endregion
    }
}
