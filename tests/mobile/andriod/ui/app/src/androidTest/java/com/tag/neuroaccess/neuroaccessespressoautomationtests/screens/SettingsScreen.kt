package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.scrollTo
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object SettingsScreen {
    const val SCREEN = "screen_settings"
    private const val CHANGE_PIN = "button_settings_change_pin"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(SCREEN)).check(matches(isDisplayed()))
    }

    fun openChangePin() {
        ScreenWaiter.performActionAndWaitFor(PinAuthenticationPopup.POPUP) {
            onView(withAutomationId(CHANGE_PIN))
                .perform(scrollTo(), click())
        }
    }
}
