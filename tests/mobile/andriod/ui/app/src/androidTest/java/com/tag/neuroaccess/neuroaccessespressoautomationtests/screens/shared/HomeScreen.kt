package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.ApplicationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.ViewIdentityScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.options.SettingsScreen

import android.view.KeyEvent
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText

object HomeScreen {
    const val SCREEN = UiIds.ScreenMainHome
    private const val APPLY_FOR_PERSONAL_ID = UiIds.ButtonMainHomeApplyidentity
    private const val SETTINGS = UiIds.ButtonMainHomeSettings
    private const val SHOW_ID = UiIds.ButtonMainHomeShowidentity

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        UiContract.assertScreen(SCREEN)
    }

    /** Verifies the actual post-onboarding Home actions without opening an identity or changing state. */
    fun assertFreshAccount() {
        ScreenWaiter.waitUntilReady(SHOW_ID)
        UiContract.assertAction(SHOW_ID, UiText.text("ShowAccount"), enabled = true, screenId = SCREEN)
        UiContract.assertAction(UiIds.ButtonMainHomeScanqr, UiText.text("ScanQRShort"), enabled = true, screenId = SCREEN)
        UiContract.assertAction(APPLY_FOR_PERSONAL_ID, UiText.text("GoToApplications"), enabled = true, screenId = SCREEN)
    }

    /** Returns from a persisted child page to the home screen without clearing account state. */
    fun returnToHome() {
        val instrumentation = androidx.test.platform.app.InstrumentationRegistry.getInstrumentation()
        repeat(8) {
            if (ScreenWaiter.isDisplayed(SCREEN)) {
                return
            }
            instrumentation.sendKeyDownUpSync(KeyEvent.KEYCODE_BACK)
            android.os.SystemClock.sleep(300L)
        }
        ScreenWaiter.waitForAnyLive(SCREEN)
    }
    fun openPersonalIdApplications() {
        ScreenWaiter.performActionAndWaitFor(ApplicationsScreen.SCREEN) {
            onView(withAutomationId(APPLY_FOR_PERSONAL_ID, SCREEN))
                .perform(click())
        }
    }

    fun openSettings() {
        ScreenWaiter.performActionAndWaitFor(SettingsScreen.SCREEN) {
            onView(withAutomationId(SETTINGS, SCREEN))
                .perform(click())
        }
    }

    fun openPersonalIdAndWaitForPinPrompt() {
        ScreenWaiter.performActionAndWaitFor(PinAuthenticationPopup.POPUP) {
            onView(withAutomationId(SHOW_ID, SCREEN))
                .perform(click())
        }
    }

    fun openPersonalId(pin: String) {
        ScreenWaiter.clickToLiveScreen(SHOW_ID, SCREEN)
        if (ScreenWaiter.waitForAnyLive(ViewIdentityScreen.SCREEN, PinAuthenticationPopup.POPUP) ==
            PinAuthenticationPopup.POPUP) {
            PinAuthenticationPopup.enterPinAndWaitFor(pin, ViewIdentityScreen.SCREEN)
        }
        ViewIdentityScreen.assertDisplayed()
    }

    fun openPersonalIdWithAuthenticatedSession() {
        ScreenWaiter.performActionAndWaitFor(ViewIdentityScreen.SCREEN) {
            onView(withAutomationId(SHOW_ID, SCREEN))
                .perform(click())
        }
    }
}
