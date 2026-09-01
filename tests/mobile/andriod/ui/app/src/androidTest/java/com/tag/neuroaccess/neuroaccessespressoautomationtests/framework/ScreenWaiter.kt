package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.app.Activity
import android.os.Looper
import android.view.View
import android.view.ViewGroup
import android.view.ViewTreeObserver
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
        this.withScreenWaiter(screenAutomationId) {
            onView(withContentDescription(screenAutomationId))
                .check(matches(isDisplayed()))
        }
    }

    fun performActionAndWaitFor(screenAutomationId: String, action: () -> Unit) {
        action()
        this.waitFor(screenAutomationId)
    }

    private fun withScreenWaiter(screenAutomationId: String, interaction: () -> Unit) {
        val screenIdlingResource = ContentDescriptionIdlingResource(screenAutomationId)
        IdlingRegistry.getInstance().register(screenIdlingResource)

        try {
            interaction()
        } finally {
            IdlingRegistry.getInstance().unregister(screenIdlingResource)
            screenIdlingResource.close()
        }
    }
}

private class ContentDescriptionIdlingResource(
    private val expectedContentDescription: String
) : IdlingResource {

    @Volatile
    private var callback: IdlingResource.ResourceCallback? = null

    @Volatile
    private var isTargetDisplayed = false

    private var observedRootView: View? = null

    private val layoutListener = ViewTreeObserver.OnGlobalLayoutListener {
        this.updateIdleState()
    }

    init {
        this.runOnMainThread {
            this.updateIdleState()
        }
    }

    override fun getName(): String = "Visible content description: $expectedContentDescription"

    override fun isIdleNow(): Boolean {
        this.runOnMainThread {
            this.updateIdleState()
        }

        return this.isTargetDisplayed
    }

    override fun registerIdleTransitionCallback(callback: IdlingResource.ResourceCallback) {
        this.callback = callback
        if (this.isTargetDisplayed) {
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
        this.isTargetDisplayed = isDisplayed
        if (isDisplayed) {
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
