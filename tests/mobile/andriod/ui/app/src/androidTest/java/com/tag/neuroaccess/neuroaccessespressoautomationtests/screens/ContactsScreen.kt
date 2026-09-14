package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScrollableScreen
import org.hamcrest.Matchers.equalTo

/** Opens saved contacts through the app list and selects rows by their actual legal identity. */
object ContactsScreen {
    private const val SCREEN = "screen_contacts"

    /** Opens Apps → Contacts from Home and locates the exact saved identity row. */
    fun openFromHome(peerIdentity: String) {
        HomeScreen.assertDisplayed()
        ContactIdentityScreen.tapText("Apps")
        ScreenWaiter.waitForAnyLive("screen_apps")
        ScreenWaiter.clickToLiveScreen("button_apps_contacts")
        ScreenWaiter.waitForAnyLive(SCREEN)
        ScrollableScreen.waitForVisibleMatch(SCREEN, "scroll_contacts", "the saved peer contact") {
            AutomationIdMatcher.matches(it, "contact_row_$peerIdentity")
        }
    }

    /** Opens the saved contact's ID and compares the visible Neuro-ID with the exporting device. */
    fun verifySavedIdentity(peerIdentity: String) {
        openFromHome(peerIdentity)
        ScreenWaiter.clickToLiveScreen("button_contact_details_$peerIdentity")
        ViewIdentityScreen.assertDisplayed()
        ScreenWaiter.waitForLiveText("displayed_identity_$peerIdentity", equalTo("Approved"))
        val actual = IdentityDetailsScreen.openAndReadNeuroId()
        check(actual == peerIdentity) { "The saved contact's Neuro-ID differs from the other device." }
    }

    /** Opens the chat button belonging to the exact saved identity row. */
    fun openChatFromHome(peerIdentity: String) {
        openFromHome(peerIdentity)
        ScreenWaiter.clickToLiveScreen("button_contact_chat_$peerIdentity")
        ChatScreen.assertPeer(peerIdentity)
    }
}
