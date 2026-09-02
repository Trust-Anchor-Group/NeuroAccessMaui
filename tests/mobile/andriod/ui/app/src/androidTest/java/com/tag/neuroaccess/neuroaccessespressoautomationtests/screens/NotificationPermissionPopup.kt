package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object NotificationPermissionPopup {
    private const val POPUP = "popup_notification_permission"
    private const val SKIP = "button_notification_permission_skip"

    fun dismissIfDisplayed() {
        ScreenWaiter.waitForIdle()
        if (ScreenWaiter.isDisplayed(POPUP)) {
            onView(withContentDescription(SKIP))
                .check(matches(isDisplayed()))
                .perform(click())
        }
    }
}