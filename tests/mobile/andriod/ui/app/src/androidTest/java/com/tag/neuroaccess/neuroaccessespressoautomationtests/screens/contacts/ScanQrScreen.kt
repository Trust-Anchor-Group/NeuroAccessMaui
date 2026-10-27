package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.contacts

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.QuickLoginClient
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.HomeScreen

/** Interacts with the real scanner icon and manual input, independently of translated labels. */
object ScanQrScreen {
    const val SCREEN = UiIds.ScreenMainQrscan

    /** Opens the scanner through Home's real QR action. Camera permission is a run prerequisite. */
    fun openFromHome() {
        HomeScreen.assertDisplayed()
        UiContract.assertAction(UiIds.ButtonMainHomeScanqr, UiText.text("ScanQRShort"), true, HomeScreen.SCREEN)
        ScreenWaiter.clickToLiveScreen(UiIds.ButtonMainHomeScanqr, HomeScreen.SCREEN)
        ScreenWaiter.waitForAnyLive(SCREEN)
        UiContract.assertScreen(SCREEN)
    }

    /** Selects manual mode through its identified icon, not through a nonexistent text button. */
    fun selectManualEntry() {
        ScreenWaiter.waitForAnyLive(SCREEN)
        UiContract.assertScreen(SCREEN)
        ScreenWaiter.waitUntilReady(UiIds.ButtonMainQrscanManualentry, SCREEN)
        UiContract.assertIcon(UiIds.ButtonMainQrscanManualentry, enabled = true, screenId = SCREEN)
        ScreenWaiter.clickToLiveScreen(UiIds.ButtonMainQrscanManualentry, SCREEN)
        ScreenWaiter.waitFor(UiIds.ControlMainQrscanManual, SCREEN)
        UiContract.assertLabel(UiIds.LabelMainQrscanPayload, UiText.text("QrScanUri"), SCREEN)
        UiContract.assertInput(UiIds.InputMainQrscanPayload, enabled = true, screenId = SCREEN)
    }

    /** Requests a configured real login after the caller has opened the scanner. */
    fun openQuickLogin(nextScreen: String, endpoint: String = TestData.quickLoginEndpoint(), tabId: String = "") {
        require(nextScreen.isNotBlank()) { "An expected destination screen is required." }
        this.selectManualEntry()
        this.enterLinkAndOpen(QuickLoginClient.fetchSignUrl(endpoint = endpoint, tabId = tabId), nextScreen)
    }

    /** Enters the real QR payload with redacted diagnostics and submits its production action. */
    fun enterLinkAndOpen(signUrl: String, nextScreen: String) {
        require(signUrl.isNotBlank()) { "A QR payload is required." }
        require(nextScreen.isNotBlank()) { "An expected destination screen is required." }
        ScreenWaiter.waitFor(UiIds.ControlMainQrscanManual, SCREEN)
        UiContract.assertLabel(UiIds.LabelMainQrscanPayload, UiText.text("QrScanUri"), SCREEN)
        UiContract.enterText(UiIds.InputMainQrscanPayload, signUrl, SCREEN)
        ScreenWaiter.waitUntilReady(UiIds.ButtonMainQrscanOpen, SCREEN)
        UiContract.assertAction(UiIds.ButtonMainQrscanOpen, UiText.text("Open"), true, SCREEN)
        ScreenWaiter.clickToLiveScreen(UiIds.ButtonMainQrscanOpen, SCREEN)
        ScreenWaiter.waitForAnyLive(nextScreen)
    }
}
