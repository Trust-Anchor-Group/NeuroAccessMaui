package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScrollableScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import org.hamcrest.Matchers.equalTo

/** Follows real application commands without picking the first template or encoding personal data in IDs. */
object ApplicationsScreen {
    const val SCREEN = UiIds.ScreenApplicationsCatalog
    private const val CURRENT_APPLICATION = UiIds.ButtonApplicationsCatalogCurrent

    fun assertDisplayed() {
        ScreenWaiter.waitForAnyLive(SCREEN)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelApplicationsCatalogTitle, UiText.text("Applications"), SCREEN)
    }

    /** Requires an explicitly configured server template and its independent localized display name. */
    fun openNewPersonalIdApplication() {
        val itemKey = TestData.kycTemplateItemId()
        val expectedName = TestData.kycTemplateName()
        val item = UiIds.forKey(UiIds.ItemApplicationsCatalogTemplate, itemKey)
        assertDisplayed()
        ScrollableScreen.waitForVisibleMatch(SCREEN, UiIds.ControlApplicationsCatalogScroll,
            "the configured application template") { AutomationIdMatcher.matches(it, item) }
        UiContract.assertIcon(item, enabled = true, screenId = SCREEN)
        UiContract.assertLabel(UiIds.forKey(UiIds.LabelApplicationsCatalogTemplatename, itemKey),
            expectedName, SCREEN)
        UiContract.assertLabel(UiIds.forKey(UiIds.LabelApplicationsCatalogTemplatetype, itemKey),
            UiText.text("IdentityApplication"), SCREEN)
        ScreenWaiter.clickToLiveScreen(item, SCREEN)
        // The configured fixture must be a photo-based template. Never bypass a document/NFC requirement.
        ScreenWaiter.waitForAnyLive(KycProcessScreen.SCREEN)
    }

    /** Reads the approved ID from the visible production card, then returns to the catalogue. */
    fun currentIdentityId(): String {
        openApprovedIdentity()
        val identityId = IdentityDetailsScreen.openAndReadNeuroId()
        ViewIdentityScreen.closeAndWaitFor(SCREEN)
        assertDisplayed()
        return identityId
    }

    fun assertCurrentApproved() {
        assertDisplayed()
        ScreenWaiter.waitForLiveText(UiIds.LabelApplicationsCatalogCurrentstate,
            equalTo(UiText.text("IdentityState_Approved")), SCREEN)
        UiContract.assertLabel(UiIds.LabelApplicationsCatalogCurrentstate,
            UiText.text("IdentityState_Approved"), SCREEN)
        UiContract.assertIcon(CURRENT_APPLICATION, enabled = true, screenId = SCREEN)
    }

    fun openApprovedIdentity() {
        assertCurrentApproved()
        ScreenWaiter.clickToLiveScreen(CURRENT_APPLICATION, SCREEN)
        ViewIdentityScreen.assertApproved()
    }

    /** Resumes an existing draft or review; a missing current application never selects a different template. */
    fun openPersonalIdApplication() {
        assertDisplayed()
        ScreenWaiter.waitUntilReady(CURRENT_APPLICATION, SCREEN)
        val state = UiContract.readVisibleLabel(UiIds.LabelApplicationsCatalogCurrentstate, SCREEN)
        val draft = UiText.text("InProgress")
        val review = UiText.text("InReview")
        check(state == draft || state == review) {
            "PRECONDITION: resuming requires a draft or pending-review application."
        }
        UiContract.assertLabel(UiIds.LabelApplicationsCatalogCurrentstate,
            if (state == draft) draft else review, SCREEN)
        val reviewTitle = if (state == review) {
            if (TestData.kycReviewStage() == "finalizing") UiText.text("IdentityApplication")
            else UiText.text("IdentityReviewRequest")
        } else null
        UiContract.assertIcon(CURRENT_APPLICATION, enabled = true, screenId = SCREEN)
        ScreenWaiter.clickToLiveScreen(CURRENT_APPLICATION, SCREEN)
        if (state == review) {
            ScreenWaiter.waitForAnyLive(UiIds.ScreenKycStatus)
            UiContract.assertScreen(UiIds.ScreenKycStatus)
            UiContract.assertLabel(UiIds.LabelKycStatusHeading, UiText.text("IdentityApplication"), UiIds.ScreenKycStatus)
            UiContract.assertLabel(UiIds.LabelKycStatusTitle, checkNotNull(reviewTitle), UiIds.ScreenKycStatus)
            UiContract.assertAction(UiIds.ButtonKycStatusApplications, UiText.text("Applications"),
                enabled = true, screenId = UiIds.ScreenKycStatus)
            UiContract.assertAction(UiIds.ButtonKycStatusOpen, UiText.text("Open"),
                enabled = true, screenId = UiIds.ScreenKycStatus)
            ScreenWaiter.clickToLiveScreen(UiIds.ButtonKycStatusOpen, UiIds.ScreenKycStatus)
        }
        ScreenWaiter.waitForAnyLive(KycProcessScreen.SCREEN)
    }
}
