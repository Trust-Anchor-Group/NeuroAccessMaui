package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.os.Handler
import android.os.Looper
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.test.espresso.Espresso.onIdle
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.IdlingPolicies
import androidx.test.espresso.IdlingRegistry
import androidx.test.espresso.IdlingResource
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed as espressoIsDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isDisplayingAtLeast
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import org.hamcrest.Matcher
import org.hamcrest.Matchers.allOf
import java.util.concurrent.TimeUnit

/**
 * Waits for view conditions in resumed activities, including MAUI popups hosted in those activities.
 * Native dialogs have separate window roots and must be handled by their dialog-specific helpers.
 */
object ScreenWaiter {
    private const val IDLING_RESOURCE_TIMEOUT_SECONDS = 120L
    private const val CLICK_VISIBLE_PERCENTAGE = 90

    init {
        IdlingPolicies.setIdlingResourceTimeout(IDLING_RESOURCE_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        IdlingPolicies.setMasterPolicyTimeout(IDLING_RESOURCE_TIMEOUT_SECONDS, TimeUnit.SECONDS)
    }

    fun clickToLiveScreen(automationId: String) {
        var point: android.graphics.Point? = null
        awaitLiveCondition("clickable area of $automationId") {
            onMainThread {
                val view = resumedRoots().firstNotNullOfOrNull { root ->
                    findView(root) { candidate ->
                        AutomationIdMatcher.matches(candidate, automationId) &&
                            isEnabled().matches(candidate) &&
                            isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(candidate)
                    }
                }
                val bounds = android.graphics.Rect()
                point = if (view != null && view.getGlobalVisibleRect(bounds))
                    android.graphics.Point(bounds.centerX(), bounds.centerY()) else null
                point != null
            }
        }
        val target = checkNotNull(point)
        val automation = InstrumentationRegistry.getInstrumentation().uiAutomation
        val downTime = android.os.SystemClock.uptimeMillis()
        fun inject(action: Int) {
            val event = android.view.MotionEvent.obtain(downTime,
                android.os.SystemClock.uptimeMillis(), action, target.x.toFloat(), target.y.toFloat(), 0)
            event.source = android.view.InputDevice.SOURCE_TOUCHSCREEN
            try {
                check(automation.injectInputEvent(event, true)) { "Could not inject tap on $automationId." }
            } finally {
                event.recycle()
            }
        }
        inject(android.view.MotionEvent.ACTION_DOWN)
        inject(android.view.MotionEvent.ACTION_UP)
    }
    fun waitForAnyLive(vararg automationIds: String): String {
        require(automationIds.isNotEmpty()) { "At least one AutomationId is required." }
        var selected: String? = null
        awaitLiveCondition("visible screen among ${automationIds.toList()}") {
            selected = automationIds.firstOrNull { isDisplayed(it) }
            selected != null
        }
        return checkNotNull(selected)
    }

    fun waitForLiveText(automationId: String, expectedText: Matcher<String>) {
        awaitLiveCondition("$automationId displaying $expectedText") {
            hasView { view ->
                AutomationIdMatcher.matches(view, automationId) &&
                    espressoIsDisplayed().matches(view) && withText(expectedText).matches(view)
            }
        }
    }

    private fun awaitLiveCondition(description: String, condition: () -> Boolean) {
        check(Looper.myLooper() != Looper.getMainLooper()) { "Live waits must run on the instrumentation thread." }
        val deadline = android.os.SystemClock.uptimeMillis() + 15_000L
        do {
            if (condition()) return
            android.os.SystemClock.sleep(100L)
        } while (android.os.SystemClock.uptimeMillis() < deadline)
        error("Timed out waiting for $description on the animated screen.")
    }
    /** Waits for an identified view to satisfy Espresso's displayed matcher. */
    fun waitFor(screenAutomationId: String) {
        this.waitForMatcher(
            "Displayed automation ID: $screenAutomationId",
            allOf(AutomationIdMatcher.withAutomationId(screenAutomationId), espressoIsDisplayed())
        )
    }

    /** Waits for displayed text in an activity root using the same matcher as the assertion. */
    fun waitForText(expectedText: String) {
        this.waitForMatcher("Displayed text: $expectedText", allOf(withText(expectedText), espressoIsDisplayed()))
    }

    fun waitForText(automationId: String, expectedText: Matcher<String>) {
        this.waitForMatcher("Text on automation ID: $automationId", allOf(
            AutomationIdMatcher.withAutomationId(automationId),
            withText(expectedText), espressoIsDisplayed()
        ))
    }
    /** Waits for a displayed, enabled control; use [waitUntilReady] before clicking it. */
    fun waitUntilEnabled(automationId: String) {
        this.waitForMatcher(
            "Displayed and enabled automation ID: $automationId",
            allOf(AutomationIdMatcher.withAutomationId(automationId), espressoIsDisplayed(), isEnabled())
        )
    }

    /** Waits for an enabled control with at least 90 percent visible, matching Espresso click constraints. */
    fun waitUntilReady(automationId: String) {
        this.waitForMatcher(
            "Ready automation ID: $automationId",
            allOf(AutomationIdMatcher.withAutomationId(automationId), isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE), isEnabled())
        )
    }

    /** Returns the first requested ID that is displayed in a resumed activity. */
    fun waitForAny(vararg screenAutomationIds: String): String {
        require(screenAutomationIds.isNotEmpty()) { "At least one AutomationId is required." }
        var selectedId: String? = null
        this.awaitCondition("Displayed automation ID: one of ${screenAutomationIds.toList()}") { roots ->
            selectedId = screenAutomationIds.firstOrNull { id ->
                roots.any { root ->
                    findView(root) { view ->
                        espressoIsDisplayed().matches(view) && AutomationIdMatcher.matches(view, id)
                    } != null
                }
            }
            selectedId != null
        }
        return checkNotNull(selectedId)
    }

    fun waitForAnyReady(vararg automationIds: String): String {
        require(automationIds.isNotEmpty()) { "At least one AutomationId is required." }
        var selectedId: String? = null
        this.awaitCondition("Ready automation ID: one of ${automationIds.toList()}") { roots ->
            selectedId = automationIds.firstOrNull { id ->
                roots.any { root ->
                    findView(root) { view ->
                        isEnabled().matches(view) &&
                            isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(view) &&
                            AutomationIdMatcher.matches(view, id)
                    } != null
                }
            }
            selectedId != null
        }
        return checkNotNull(selectedId)
    }
    /** Waits for Espresso's tracked work; this alone does not establish completion of MAUI operations. */
    fun waitForIdle() {
        onIdle()
    }

    /** Reports whether a view exists in a shown layout, even when scrolled outside the viewport. */
    fun isPresentInLayout(automationId: String): Boolean = this.hasView { view ->
        view.isShown && AutomationIdMatcher.matches(view, automationId)
    }

    /** Reports whether an identified view satisfies Espresso's displayed matcher. */
    fun isDisplayed(automationId: String): Boolean = this.hasView { view ->
        espressoIsDisplayed().matches(view) && AutomationIdMatcher.matches(view, automationId)
    }

    /** Reports whether at least 90 percent of the identified view is visible, without checking enabled state. */
    fun isDisplayedOnScreen(automationId: String): Boolean = this.hasView { view ->
        isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(view) && AutomationIdMatcher.matches(view, automationId)
    }

    /** Returns matching enabled text with at least 90 percent visible, in view-tree order. */
    fun firstDisplayedEnabledText(vararg expectedTexts: String): String? = onMainThread {
        val expected = expectedTexts.toSet()
        resumedRoots().firstNotNullOfOrNull { root ->
            (findView(root) { view ->
                view is TextView && view.text?.toString() in expected &&
                    isEnabled().matches(view) && isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(view)
            } as? TextView)?.text?.toString()
        }
    }

    /** Waits until no matching view is displayed; an activity transition with no resumed root is not success. */
    fun waitUntilHidden(automationId: String) {
        this.awaitCondition("Hidden automation ID: $automationId") { roots ->
            roots.none { root ->
                findView(root) { view ->
                    espressoIsDisplayed().matches(view) && AutomationIdMatcher.matches(view, automationId)
                } != null
            }
        }
    }

    /** Performs an action before waiting for its destination, avoiding an idling dependency on the action itself. */
    fun performActionAndWaitFor(screenAutomationId: String, action: () -> Unit) {
        action()
        this.waitFor(screenAutomationId)
    }

    private fun waitForMatcher(description: String, matcher: Matcher<View>) {
        this.awaitCondition(description) { roots ->
            roots.any { root -> findView(root, matcher::matches) != null }
        }
        onView(matcher).check(matches(matcher))
    }

    private fun hasView(predicate: (View) -> Boolean): Boolean = onMainThread {
        resumedRoots().any { root -> findView(root, predicate) != null }
    }

    private fun awaitCondition(description: String, condition: (List<View>) -> Boolean) {
        val resource = ViewConditionIdlingResource(description) {
            val roots = resumedRoots()
            roots.isNotEmpty() && condition(roots)
        }
        try {
            IdlingRegistry.getInstance().register(resource)
            resource.start()
            onIdle()
            resource.failure?.let { throw it }
        } finally {
            IdlingRegistry.getInstance().unregister(resource)
            resource.close()
        }
    }
}

/** Returns fresh roots on each observation, so activity changes do not leave stale view references. */
private fun resumedRoots(): List<View> =
    ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
        .map { it.window.decorView }

/** Finds the first matching view without defining a separate visibility policy. */
private fun findView(view: View, predicate: (View) -> Boolean): View? {
    if (predicate(view)) return view
    if (view is ViewGroup) {
        for (index in 0 until view.childCount) {
            findView(view.getChildAt(index), predicate)?.let { return it }
        }
    }
    return null
}

/** Evaluates a view query on the UI thread and propagates failures to the instrumentation thread. */
private fun <T> onMainThread(action: () -> T): T {
    if (Looper.myLooper() == Looper.getMainLooper()) return action()
    var result: Result<T>? = null
    InstrumentationRegistry.getInstrumentation().runOnMainSync { result = runCatching(action) }
    return checkNotNull(result).getOrThrow()
}

/**
 * Observes one view condition until satisfied. Polling is confined to the registered wait and
 * catches enabled/text changes without requiring layout events or retaining activity roots.
 */
private class ViewConditionIdlingResource(
    private val description: String,
    private val condition: () -> Boolean
) : IdlingResource {
    private val handler = Handler(Looper.getMainLooper())
    private var closed = false

    @Volatile
    private var idle = false

    @Volatile
    private var callback: IdlingResource.ResourceCallback? = null

    /** Query failures are rethrown on the test thread after Espresso has been released. */
    @Volatile
    var failure: Exception? = null
        private set

    private val observation = object : Runnable {
        override fun run() {
            if (closed || idle) return
            val satisfied = try {
                condition()
            } catch (exception: Exception) {
                failure = exception
                true
            }
            if (satisfied) {
                idle = true
                // One-shot transition; all state updates precede notification.
                callback?.onTransitionToIdle()
            } else {
                handler.postDelayed(this, OBSERVATION_INTERVAL_MILLISECONDS)
            }
        }
    }

    override fun getName(): String = description

    override fun isIdleNow(): Boolean = idle

    override fun registerIdleTransitionCallback(callback: IdlingResource.ResourceCallback) {
        this.callback = callback
    }

    /** Starts observing after registration; initially satisfied conditions are read by Espresso as idle. */
    fun start() {
        onMainThread { observation.run() }
    }

    /** Cancels pending observations on success, timeout or assertion failure. */
    fun close() {
        onMainThread {
            closed = true
            handler.removeCallbacks(observation)
            callback = null
        }
    }

    private companion object {
        const val OBSERVATION_INTERVAL_MILLISECONDS = 100L
    }
}
