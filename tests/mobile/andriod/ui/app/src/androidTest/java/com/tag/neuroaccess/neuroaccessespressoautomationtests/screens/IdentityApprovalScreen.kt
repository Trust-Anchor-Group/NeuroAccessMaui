package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.os.Bundle
import android.os.SystemClock
import android.view.Window
import android.view.WindowManager
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.RootMatchers.withDecorView
import androidx.test.espresso.matcher.ViewMatchers.hasDescendant
import androidx.test.espresso.matcher.ViewMatchers.isDescendantOfA
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

object IdentityApprovalScreen {
    private const val TITLE = "Unlocked"
    private const val MESSAGE =
        "You can now scan the QR-codes of other digital identities, to request more information about them."
    private const val CURRENT_APPLICATION = "button_current_identity_application"

    fun waitForApproval(timeoutMilliseconds: Long, expectUnlockedDialog: Boolean = true) {
        require(timeoutMilliseconds > 0) { "Approval timeout must be positive." }
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val windows = mutableListOf<Window>()
        instrumentation.runOnMainSync {
            ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
                .forEach { activity ->
                    val window = activity.window
                    if (window.attributes.flags and WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON == 0) {
                        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
                        windows.add(window)
                    }
                }
        }
        try {
            instrumentation.sendStatus(2, Bundle().apply {
                putString("stream", "\nWaiting for manual admin approval (up to ${timeoutMilliseconds / 60_000} minutes).\n")
            })
            if (expectUnlockedDialog) {
                this.waitForUnlockedDialog(timeoutMilliseconds)
                val dialog = allOf(
                    isDialog(),
                    withDecorView(allOf(
                        hasDescendant(withText(TITLE)),
                        hasDescendant(allOf(withId(android.R.id.message), withText(MESSAGE)))
                    ))
                )
                // MAUI's single-button DisplayAlert uses Android's negative button.
                onView(allOf(withId(android.R.id.button2), isDisplayed(), isEnabled()))
                    .inRoot(dialog)
                    .perform(click())
            } else {
                val deadline = SystemClock.uptimeMillis() + timeoutMilliseconds
                while (!ScreenWaiter.isDisplayed(ApplicationsScreen.SCREEN)) {
                    check(SystemClock.uptimeMillis() < deadline) {
                        "Manual replacement approval timed out: the pending summary did not return to Applications."
                    }
                    SystemClock.sleep(500L)
                }
            }

            ApplicationsScreen.assertDisplayed()
            ScreenWaiter.waitForText("Approved")
            onView(allOf(
                withText("Approved"),
                isDescendantOfA(withAutomationId(CURRENT_APPLICATION)),
                isDisplayed()
            )).check(matches(isDisplayed()))
        } finally {
            instrumentation.runOnMainSync {
                windows.forEach { window ->
                    window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
                }
            }
        }
    }

    private fun waitForUnlockedDialog(timeoutMilliseconds: Long) {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.uptimeMillis() + timeoutMilliseconds
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            if (root != null) {
                val titles = root.findAccessibilityNodeInfosByText(TITLE)
                val messages = root.findAccessibilityNodeInfosByText(MESSAGE)
                val unlocked = try {
                    root.packageName?.toString() == instrumentation.targetContext.packageName &&
                        titles.any { it.isVisibleToUser && it.text?.toString() == TITLE } &&
                        messages.any { it.isVisibleToUser && it.text?.toString() == MESSAGE }
                } finally {
                    @Suppress("DEPRECATION")
                    titles.forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    messages.forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    root.recycle()
                }
                if (unlocked) return
            }
            SystemClock.sleep(500L)
        } while (SystemClock.uptimeMillis() < deadline)

        error("Manual approval timed out: the Unlocked dialog was not received. " +
            "Approve the submitted application in admin before the deadline; the app must be in English " +
            "and the profile must be transitioning to approved personal information.")
    }
}
