package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.view.View
import android.view.ViewGroup
import androidx.test.espresso.action.ViewActions.swipeUp
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import org.hamcrest.Matchers.allOf
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object ApplicationsScreen {
    const val SCREEN = "screen_identity_applications"
    private const val CURRENT_APPLICATION = "button_current_identity_application"
    private const val AVAILABLE_APPLICATION = "button_available_identity_application"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(SCREEN))
            .check(matches(isDisplayed()))
    }

    fun openNewPersonalIdApplication() {
        repeat(8) {
            if (ScreenWaiter.isDisplayedOnScreen(AVAILABLE_APPLICATION)) {
                ScreenWaiter.waitUntilReady(AVAILABLE_APPLICATION)
                ScreenWaiter.performActionAndWaitFor(KycProcessScreen.SCREEN) {
                    onView(allOf(withAutomationId(AVAILABLE_APPLICATION), isEnabled())).perform(click())
                }
                return
            }
            onView(withAutomationId(SCREEN)).perform(swipeUp())
        }
        error("No available personal ID application could be reached.")
    }

    fun currentIdentityId(): String {
        var identityId: String? = null
        onView(withAutomationId(CURRENT_APPLICATION)).check { view, exception ->
            if (exception != null) throw exception
            val ids = mutableSetOf<String>()
            fun visit(current: View) {
                val node = current.createAccessibilityNodeInfo()
                if (node != null) {
                    try {
                        val prefix = "${current.context.packageName}:id/identity_application_"
                        node.viewIdResourceName?.takeIf { it.startsWith(prefix) }
                            ?.removePrefix(prefix)?.takeIf { it.isNotBlank() }?.let(ids::add)
                    } finally {
                        @Suppress("DEPRECATION")
                        node.recycle()
                    }
                }
                if (current is ViewGroup) {
                    for (index in 0 until current.childCount) visit(current.getChildAt(index))
                }
            }
            visit(checkNotNull(view))
            check(ids.size == 1) { "Current application must expose exactly one identity ID." }
            identityId = ids.single()
        }
        return checkNotNull(identityId)
    }
    fun openApprovedIdentity() {
        ScreenWaiter.clickToLiveScreen(CURRENT_APPLICATION)
        ViewIdentityScreen.assertDisplayed()
    }

    fun openPersonalIdApplication() {
        val applicationAutomationId = ScreenWaiter.waitForAny(
            CURRENT_APPLICATION,
            AVAILABLE_APPLICATION
        )

        ScreenWaiter.performActionAndWaitFor(KycProcessScreen.SCREEN) {
            onView(withAutomationId(applicationAutomationId))
                .perform(click())
        }
    }
}
