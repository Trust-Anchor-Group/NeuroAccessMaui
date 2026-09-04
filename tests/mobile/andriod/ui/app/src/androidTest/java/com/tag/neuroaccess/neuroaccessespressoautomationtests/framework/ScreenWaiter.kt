package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.app.Activity
import android.graphics.Rect
import android.os.Looper
import android.view.View
import android.view.ViewGroup
import android.view.ViewTreeObserver
import androidx.test.espresso.Espresso.onIdle
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.IdlingPolicies
import androidx.test.espresso.IdlingRegistry
import androidx.test.espresso.IdlingResource
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import java.util.concurrent.TimeUnit

object ScreenWaiter {

    private const val IDLING_RESOURCE_TIMEOUT_SECONDS = 120L

    init {
        IdlingPolicies.setIdlingResourceTimeout(
            IDLING_RESOURCE_TIMEOUT_SECONDS,
            TimeUnit.SECONDS
        )
        IdlingPolicies.setMasterPolicyTimeout(
            IDLING_RESOURCE_TIMEOUT_SECONDS,
            TimeUnit.SECONDS
        )
    }

    fun waitFor(screenAutomationId: String) {
        this.withContentDescriptionWaiter(screenAutomationId, true) {
            onView(withContentDescription(screenAutomationId))
                .check(matches(isDisplayed()))
        }
    }

    fun waitUntilEnabled(automationId: String) {
        this.withContentDescriptionWaiter(automationId, true, true) {
            onView(withContentDescription(automationId))
                .check(matches(isEnabled()))
        }
    }

    fun waitForAny(vararg screenAutomationIds: String): String {
        require(screenAutomationIds.isNotEmpty()) { "At least one AutomationId is required." }

        val idlingResource = AnyContentDescriptionIdlingResource(screenAutomationIds.toSet())
        IdlingRegistry.getInstance().register(idlingResource)

        try {
            onIdle()
            return checkNotNull(idlingResource.visibleAutomationId) {
                "No expected AutomationId was visible."
            }
        } finally {
            IdlingRegistry.getInstance().unregister(idlingResource)
            idlingResource.close()
        }
    }

    fun waitForIdle() {
        onIdle()
    }

    fun isDisplayed(automationId: String): Boolean {
        var isDisplayed = false

        InstrumentationRegistry.getInstrumentation().runOnMainSync {
            val resumedActivity = ActivityLifecycleMonitorRegistry.getInstance()
                .getActivitiesInStage(Stage.RESUMED)
                .firstOrNull()

            isDisplayed = resumedActivity?.window?.decorView
                ?.let { rootView -> this.hasVisibleContentDescription(rootView, automationId) }
                ?: false
        }

        return isDisplayed
    }

    fun isDisplayedOnScreen(automationId: String): Boolean {
        var isDisplayedOnScreen = false

        InstrumentationRegistry.getInstrumentation().runOnMainSync {
            val resumedActivity = ActivityLifecycleMonitorRegistry.getInstance()
                .getActivitiesInStage(Stage.RESUMED)
                .firstOrNull()

            isDisplayedOnScreen = resumedActivity?.window?.decorView
                ?.let { rootView -> this.hasDisplayedContentDescription(rootView, automationId) }
                ?: false
        }

        return isDisplayedOnScreen
    }

    private fun hasVisibleContentDescription(view: View, automationId: String): Boolean {
        if (view.visibility == View.VISIBLE && view.contentDescription?.toString() == automationId) {
            return true
        }

        if (view is ViewGroup) {
            for (index in 0 until view.childCount) {
                if (this.hasVisibleContentDescription(view.getChildAt(index), automationId)) {
                    return true
                }
            }
        }

        return false
    }

    private fun hasDisplayedContentDescription(view: View, automationId: String): Boolean {
        if (view.contentDescription?.toString() == automationId && this.isMostlyVisible(view)) {
            return true
        }

        if (view is ViewGroup) {
            for (index in 0 until view.childCount) {
                if (this.hasDisplayedContentDescription(view.getChildAt(index), automationId)) {
                    return true
                }
            }
        }

        return false
    }

    private fun isMostlyVisible(view: View): Boolean {
        if (!view.isShown || view.width <= 0 || view.height <= 0) {
            return false
        }

        val visibleBounds = Rect()
        if (!view.getGlobalVisibleRect(visibleBounds)) {
            return false
        }

        val visibleArea = visibleBounds.width().toLong() * visibleBounds.height().toLong()
        val totalArea = view.width.toLong() * view.height.toLong()
        return visibleArea * 100 >= totalArea * 90
    }

    fun waitUntilHidden(automationId: String) {
        this.withContentDescriptionWaiter(automationId, false) {
            onIdle()
        }
    }

    fun performActionAndWaitFor(screenAutomationId: String, action: () -> Unit) {
        action()
        this.waitFor(screenAutomationId)
    }

    private fun withContentDescriptionWaiter(
        automationId: String,
        shouldBeDisplayed: Boolean,
        shouldBeEnabled: Boolean = false,
        interaction: () -> Unit
    ) {
        val idlingResource = ContentDescriptionIdlingResource(
            automationId,
            shouldBeDisplayed,
            shouldBeEnabled
        )
        IdlingRegistry.getInstance().register(idlingResource)

        try {
            interaction()
        } finally {
            IdlingRegistry.getInstance().unregister(idlingResource)
            idlingResource.close()
        }
    }
}

private class ContentDescriptionIdlingResource(
    private val expectedContentDescription: String,
    private val shouldBeDisplayed: Boolean,
    private val shouldBeEnabled: Boolean
) : IdlingResource {

    @Volatile
    private var callback: IdlingResource.ResourceCallback? = null

    @Volatile
    private var isIdle = false

    private var observedRootView: View? = null

    private val layoutListener = ViewTreeObserver.OnGlobalLayoutListener {
        this.updateIdleState()
    }

    init {
        this.runOnMainThread {
            this.updateIdleState()
        }
    }

    override fun getName(): String {
        val expectedState = if (this.shouldBeDisplayed) "Visible" else "Hidden"
        val enabledState = if (this.shouldBeEnabled) " and enabled" else ""
        return "$expectedState$enabledState content description: $expectedContentDescription"
    }

    override fun isIdleNow(): Boolean {
        this.runOnMainThread {
            this.updateIdleState()
        }

        return this.isIdle
    }

    override fun registerIdleTransitionCallback(callback: IdlingResource.ResourceCallback) {
        this.callback = callback
        if (this.isIdle) {
            callback.onTransitionToIdle()
        }
    }

    fun close() {
        this.runOnMainThread {
            this.removeLayoutListener()
        }
    }

    private fun runOnMainThread(action: () -> Unit) {
        if (Looper.myLooper() == Looper.getMainLooper()) {
            action()
        } else {
            InstrumentationRegistry.getInstrumentation().runOnMainSync(action)
        }
    }

    private fun updateIdleState() {
        val currentRootView = this.getCurrentRootView()
        if (currentRootView !== this.observedRootView) {
            this.removeLayoutListener()
            this.observedRootView = currentRootView
            currentRootView?.viewTreeObserver?.addOnGlobalLayoutListener(this.layoutListener)
        }

        val matchingView = currentRootView?.let(this::findVisibleContentDescription)
        this.isIdle = if (this.shouldBeDisplayed) {
            matchingView != null && (!this.shouldBeEnabled || matchingView.isEnabled)
        } else {
            matchingView == null
        }
        if (this.isIdle) {
            this.callback?.onTransitionToIdle()
        }
    }

    private fun getCurrentRootView(): View? {
        val resumedActivity: Activity? = ActivityLifecycleMonitorRegistry.getInstance()
            .getActivitiesInStage(Stage.RESUMED)
            .firstOrNull()

        return resumedActivity?.window?.decorView
    }

    private fun removeLayoutListener() {
        val observer = this.observedRootView?.viewTreeObserver
        if (observer?.isAlive == true) {
            observer.removeOnGlobalLayoutListener(this.layoutListener)
        }
    }

    private fun findVisibleContentDescription(view: View): View? {
        if (view.isShown && view.contentDescription?.toString() == this.expectedContentDescription) {
            return view
        }

        if (view is ViewGroup) {
            for (index in 0 until view.childCount) {
                val matchingView = this.findVisibleContentDescription(view.getChildAt(index))
                if (matchingView != null) {
                    return matchingView
                }
            }
        }

        return null
    }
}

private class AnyContentDescriptionIdlingResource(
    private val expectedAutomationIds: Set<String>
) : IdlingResource {

    @Volatile
    private var callback: IdlingResource.ResourceCallback? = null

    @Volatile
    var visibleAutomationId: String? = null
        private set

    private var observedRootView: View? = null

    private val layoutListener = ViewTreeObserver.OnGlobalLayoutListener {
        this.updateIdleState()
    }

    init {
        this.runOnMainThread {
            this.updateIdleState()
        }
    }

    override fun getName(): String {
        return "Visible content description: one of $expectedAutomationIds"
    }

    override fun isIdleNow(): Boolean {
        this.runOnMainThread {
            this.updateIdleState()
        }

        return this.visibleAutomationId != null
    }

    override fun registerIdleTransitionCallback(callback: IdlingResource.ResourceCallback) {
        this.callback = callback
        if (this.visibleAutomationId != null) {
            callback.onTransitionToIdle()
        }
    }

    fun close() {
        this.runOnMainThread {
            val observer = this.observedRootView?.viewTreeObserver
            if (observer?.isAlive == true) {
                observer.removeOnGlobalLayoutListener(this.layoutListener)
            }
        }
    }

    private fun runOnMainThread(action: () -> Unit) {
        if (Looper.myLooper() == Looper.getMainLooper()) {
            action()
        } else {
            InstrumentationRegistry.getInstrumentation().runOnMainSync(action)
        }
    }

    private fun updateIdleState() {
        val resumedActivity = ActivityLifecycleMonitorRegistry.getInstance()
            .getActivitiesInStage(Stage.RESUMED)
            .firstOrNull()
        val currentRootView = resumedActivity?.window?.decorView

        if (currentRootView !== this.observedRootView) {
            val previousObserver = this.observedRootView?.viewTreeObserver
            if (previousObserver?.isAlive == true) {
                previousObserver.removeOnGlobalLayoutListener(this.layoutListener)
            }

            this.observedRootView = currentRootView
            currentRootView?.viewTreeObserver?.addOnGlobalLayoutListener(this.layoutListener)
        }

        this.visibleAutomationId = currentRootView?.let { rootView ->
            this.expectedAutomationIds.firstOrNull { automationId ->
                this.hasVisibleContentDescription(rootView, automationId)
            }
        }

        if (this.visibleAutomationId != null) {
            this.callback?.onTransitionToIdle()
        }
    }

    private fun hasVisibleContentDescription(view: View, automationId: String): Boolean {
        if (view.isShown && view.contentDescription?.toString() == automationId) {
            return true
        }

        if (view is ViewGroup) {
            for (index in 0 until view.childCount) {
                if (this.hasVisibleContentDescription(view.getChildAt(index), automationId)) {
                    return true
                }
            }
        }

        return false
    }
}
