package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object HomeScreen {
    const val SCREEN = "screen_home"
    private const val APPLY_FOR_PERSONAL_ID = "button_apply_for_personal_id"
    private const val SETTINGS = "button_home_settings"
    private const val SHOW_ID = "button_home_show_id"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(SCREEN))
            .check(matches(isDisplayed()))
    }

    fun openPersonalIdApplications() {
        ScreenWaiter.performActionAndWaitFor(ApplicationsScreen.SCREEN) {
            onView(withAutomationId(APPLY_FOR_PERSONAL_ID))
                .perform(click())
        }
    }

    fun openSettings() {
        ScreenWaiter.performActionAndWaitFor(SettingsScreen.SCREEN) {
            onView(withAutomationId(SETTINGS))
                .perform(click())
        }
    }

    fun openPersonalIdAndWaitForPinPrompt() {
        ScreenWaiter.performActionAndWaitFor(PinAuthenticationPopup.POPUP) {
            onView(withAutomationId(SHOW_ID))
                .perform(click())
        }
    }

    fun openPersonalId(pin: String) {
        ScreenWaiter.clickToLiveScreen(SHOW_ID)
        if (ScreenWaiter.waitForAnyLive(ViewIdentityScreen.SCREEN, PinAuthenticationPopup.POPUP) ==
            PinAuthenticationPopup.POPUP) {
            PinAuthenticationPopup.enterPinAndWaitFor(pin, ViewIdentityScreen.SCREEN)
        }
        ViewIdentityScreen.assertDisplayed()
    }

    fun openPersonalIdWithAuthenticatedSession() {
        ScreenWaiter.performActionAndWaitFor(ViewIdentityScreen.SCREEN) {
            onView(withAutomationId(SHOW_ID))
                .perform(click())
        }
    }
}
