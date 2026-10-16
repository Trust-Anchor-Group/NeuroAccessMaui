package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.anyOf
import org.hamcrest.Matchers.equalTo
import org.hamcrest.Matchers.startsWith

object ViewIdentityScreen {
    const val SCREEN = UiIds.ScreenIdentityCard

    fun assertApprovedIdentity(identityId: String, firstName: String) {
        assertDisplayed()
        assertApproved()
        ScreenWaiter.waitForLiveText(UiIds.LabelIdentityCardName,
            anyOf(equalTo(firstName), startsWith("$firstName ")), SCREEN)
        val displayedId = IdentityDetailsScreen.openAndReadNeuroId()
        check(displayedId == identityId) { "UI: displayed identity differs from the configured identity (values redacted)." }
    }

    /** Requires the real localized state before any flow changes credentials or signs a request. */
    fun assertApproved() {
        assertDisplayed()
        ScreenWaiter.waitForLiveText(UiIds.LabelIdentityCardState, equalTo(UiText.text("IdentityState_Approved")), SCREEN)
        UiContract.assertLabel(UiIds.LabelIdentityCardState, UiText.text("IdentityState_Approved"), SCREEN)
    }

    fun assertDisplayed() {
        ScreenWaiter.waitForAnyLive(SCREEN)
        UiContract.assertScreen(SCREEN)
    }

    fun closeAndWaitFor(nextScreen: String) {
        assertDisplayed()
        UiContract.assertIcon(UiIds.ButtonIdentityCardBack, enabled = true, screenId = SCREEN)
        ScreenWaiter.clickToLiveScreen(UiIds.ButtonIdentityCardBack, SCREEN)
        ScreenWaiter.waitFor(nextScreen)
    }
}
