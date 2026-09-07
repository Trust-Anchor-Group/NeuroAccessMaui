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
    private const val SETTINGS = "button_home_settings"
    private const val SHOW_ID = "button_home_show_id"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(SCREEN))
            .check(matches(isDisplayed()))
    }

    fun openPersonalIdApplications() {
        ScreenWaiter.performActionAndWaitFor(ApplicationsScreen.SCREEN) {
            onView(withContentDescription(APPLY_FOR_PERSONAL_ID))
                .perform(click())
        }
    }

    fun openSettings() {
        ScreenWaiter.performActionAndWaitFor(SettingsScreen.SCREEN) {
            onView(withContentDescription(SETTINGS))
                .perform(click())
        }
    }

    fun openPersonalIdAndWaitForPinPrompt() {
        ScreenWaiter.performActionAndWaitFor(PinAuthenticationPopup.POPUP) {
            onView(withContentDescription(SHOW_ID))
                .perform(click())
        }
    }

    fun openPersonalIdWithAuthenticatedSession() {
        ScreenWaiter.performActionAndWaitFor(ViewIdentityScreen.SCREEN) {
            onView(withContentDescription(SHOW_ID))
                .perform(click())
        }
    }
}
