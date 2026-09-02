package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.app.Activity
import android.os.Looper
import android.view.View
import android.view.ViewGroup
import android.view.ViewTreeObserver
import androidx.test.espresso.Espresso.onIdle
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.IdlingRegistry
import androidx.test.espresso.IdlingResource
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage

object ScreenWaiter {

    fun waitFor(screenAutomationId: String) {
        this.withContentDescriptionWaiter(screenAutomationId, true) {
            onView(withContentDescription(screenAutomationId))
                .check(matches(isDisplayed()))
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
        interaction: () -> Unit
    ) {
        val idlingResource = ContentDescriptionIdlingResource(automationId, shouldBeDisplayed)
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
    private val shouldBeDisplayed: Boolean
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
        return "$expectedState content description: $expectedContentDescription"
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

        val isDisplayed = currentRootView?.let(this::hasVisibleContentDescription) == true
        this.isIdle = isDisplayed == this.shouldBeDisplayed
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

    private fun hasVisibleContentDescription(view: View): Boolean {
        if (view.visibility == View.VISIBLE && view.contentDescription?.toString() == this.expectedContentDescription) {
            return true
        }

        if (view is ViewGroup) {
            for (index in 0 until view.childCount) {
                if (this.hasVisibleContentDescription(view.getChildAt(index))) {
                    return true
                }
            }
        }

        return false
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
}