package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.os.SystemClock
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

/** Handles identity petitions delivered through the notifications page. */
object NotificationsScreen {
    /** Identifies the notifications page. */
    const val SCREEN = "screen_notifications"

    private const val OPEN_NOTIFICATIONS = "button_home_notifications"
    private const val IDENTITY_REQUEST_PREFIX = "Identity request from"
    private const val PETITION_TITLE = "Access to Identity"
    private const val AUTOMATIC_PETITION_TIMEOUT_MILLISECONDS = 120_000L

    /** Identifies the page that displays an incoming identity petition. */
    const val PETITION_SCREEN = "screen_petition_identity"

    /** Waits for an identity request, opens it, confirms it and saves the requester as a contact. */
    fun acceptIdentityRequest(pin: String) {
        if (!waitForAutomaticallyOpenedPetition()) {
            ScreenWaiter.clickToLiveScreen(OPEN_NOTIFICATIONS)
            ScreenWaiter.waitForAnyLive(SCREEN)
            ContactIdentityScreen.tapTextStartingWith(IDENTITY_REQUEST_PREFIX)
            ScreenWaiter.waitForAnyLive(PETITION_SCREEN)
        }
        if (!ContactIdentityScreen.isTextVisible("Approved")) {
            ContactIdentityScreen.tapText("Accept")
            PinAuthenticationPopup.enterPinAndWaitFor(pin, PETITION_SCREEN)
            ContactIdentityScreen.waitForText("Approved")
        }
        ContactIdentityScreen.addContact()
    }

    private fun waitForAutomaticallyOpenedPetition(): Boolean {
        val deadline = SystemClock.elapsedRealtime() + AUTOMATIC_PETITION_TIMEOUT_MILLISECONDS
        do {
            if (ScreenWaiter.isDisplayed(PETITION_SCREEN) ||
                ContactIdentityScreen.isTextVisible(PETITION_TITLE)) {
                return true
            }
            SystemClock.sleep(200L)
        } while (SystemClock.elapsedRealtime() < deadline)
        return false
    }
}