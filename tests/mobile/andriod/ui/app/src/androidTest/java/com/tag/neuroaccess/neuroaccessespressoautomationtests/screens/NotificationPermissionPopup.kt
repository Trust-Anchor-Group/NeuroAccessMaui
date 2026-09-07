package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.matcher.ViewMatchers.isDisplayingAtLeast
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

/** Handles the app's notification rationale during the first Home appearance after registration. */
object NotificationPermissionPopup {
    private const val POPUP = "popup_notification_permission"
    private const val SKIP = "button_notification_permission_skip"
    private const val HOME_BANNER = "image_home_banner"

    /**
     * Waits for the rationale or completion of the first Home permission check, then dismisses it if needed.
     * HomeViewModel initially has ThemeLoaded=false and sets it only after its awaited permission flow.
     * This signal must not be reused for later Home appearances, where ThemeLoaded can already be true.
     * Native Android permission dialogs are separate from this app-owned rationale.
     */
    fun dismissAfterRegistration() {
        when (ScreenWaiter.waitForAny(POPUP, HOME_BANNER)) {
            POPUP -> {
                ScreenWaiter.waitUntilReady(SKIP)
                onView(allOf(withAutomationId(SKIP), isDisplayingAtLeast(90), isEnabled()))
                    .perform(click())
                ScreenWaiter.waitUntilHidden(POPUP)
            }
            HOME_BANNER -> Unit
        }
        // Also await the work after dismissal; absence of the popup alone is not completion.
        ScreenWaiter.waitFor(HOME_BANNER)
        ScreenWaiter.waitUntilHidden(POPUP)
    }
}
