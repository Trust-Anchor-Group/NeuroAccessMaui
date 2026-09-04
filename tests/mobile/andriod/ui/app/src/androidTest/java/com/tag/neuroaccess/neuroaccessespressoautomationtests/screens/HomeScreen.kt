package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object HomeScreen {
    const val SCREEN = "screen_home"
    private const val APPLY_FOR_PERSONAL_ID = "button_apply_for_personal_id"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(APPLY_FOR_PERSONAL_ID)
        onView(withContentDescription(APPLY_FOR_PERSONAL_ID))
            .check(matches(isDisplayed()))
    }

    fun openPersonalIdApplications() {
        ScreenWaiter.performActionAndWaitFor(ApplicationsScreen.SCREEN) {
            onView(withContentDescription(APPLY_FOR_PERSONAL_ID))
                .perform(click())
        }
    }
}
