package com.tag.neuroaccess.neuroaccessespressoautomationtests.quicklogin

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.IdentityDetailsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.ViewIdentityScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.PinAuthenticationPopup

import android.util.Log
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.contacts.ScanQrScreen
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.equalTo
import org.junit.Test
import org.junit.runner.RunWith

/**
 * Signs into the real Quick Login page through manual QR entry and verifies its identity table.
 * Requires the configured app locale on Home with an approved identity, connectivity, camera permission,
 * PIN authentication (biometrics disabled), and the configured testPin argument.
 */
@RunWith(AndroidJUnit4::class)
class QuickLoginFlowTest : BaseTest() {
    /** Reads the mobile identity, signs in through Scan QR and compares the web identity ID. */
    @Test
    fun openQuickLoginThroughManualQrEntry() {
        Log.i("QuickLoginTest", "START: Quick Login identity comparison")
        HomeScreen.assertDisplayed()
        val pin = TestData.pin()
        TestData.quickLoginPageUrl()
        HomeScreen.openPersonalId(pin)
        ViewIdentityScreen.assertApproved()
        val identityId = IdentityDetailsScreen.openAndReadNeuroId()
        Log.i("QuickLoginTest", "MOBILE: identified Neuro-ID read (value omitted)")
        ViewIdentityScreen.closeAndWaitFor(HomeScreen.SCREEN)

        QuickLoginWebSession().use { page ->
            ScanQrScreen.openFromHome()
            ScanQrScreen.selectManualEntry()

            page.open()
            val signUrl = page.awaitSignUrl()
            val purpose = page.purpose()
            ScanQrScreen.enterLinkAndOpen(signUrl, UiIds.ScreenPetitionsSignature)
            UiContract.assertLabel(UiIds.LabelPetitionsSignatureTitle, UiText.text("Petition"), UiIds.ScreenPetitionsSignature)
            ScreenWaiter.waitForLiveText(UiIds.LabelPetitionsSignaturePurpose, equalTo(purpose), UiIds.ScreenPetitionsSignature)
            UiContract.assertLabel(UiIds.LabelPetitionsSignaturePurpose, purpose, UiIds.ScreenPetitionsSignature)
            ScreenWaiter.waitUntilReady(UiIds.ButtonPetitionsSignatureAccept, UiIds.ScreenPetitionsSignature)
            UiContract.assertAction(UiIds.ButtonPetitionsSignatureAccept, UiText.text("Accept"), true, UiIds.ScreenPetitionsSignature)
            ScreenWaiter.clickToLiveScreen(UiIds.ButtonPetitionsSignatureAccept, UiIds.ScreenPetitionsSignature)
            PinAuthenticationPopup.assertDisplayed()
            PinAuthenticationPopup.enterPinAndWaitFor(pin, HomeScreen.SCREEN)
            page.awaitApprovedIdentity(identityId)
        }
    }
}
