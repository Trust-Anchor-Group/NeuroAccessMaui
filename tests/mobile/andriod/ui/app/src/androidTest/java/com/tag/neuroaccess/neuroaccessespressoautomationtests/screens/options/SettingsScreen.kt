package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.options

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.scrollTo
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.PinAuthenticationPopup

/** Targets the current production settings, without changing labels to match legacy tests. */
object SettingsScreen {
    const val SCREEN = UiIds.ScreenMainSettings

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelMainSettingsTitle, UiText.text("Settings"), SCREEN)
    }

    fun requestIdentityRevocation() =
        selectAction(UiIds.ButtonMainSettingsRevokeidentity, UiText.text("RevokeIdentity"))

    fun openChangePin() {
        selectAction(UiIds.ButtonMainSettingsChangepassword, UiText.text("ChangePassword"))
        ScreenWaiter.waitFor(PinAuthenticationPopup.POPUP)
    }

    private fun selectAction(id: String, text: String) {
        assertDisplayed()
        try {
            onView(withAutomationId(id, SCREEN)).perform(scrollTo())
        } catch (_: Throwable) {
            throw AssertionError("TECHNICAL: settings action could not be reached.")
        }
        UiContract.assertAction(id, text, enabled = true, screenId = SCREEN)
        ScreenWaiter.clickToLiveScreen(id, SCREEN)
    }
}
