package com.tag.neuroaccess.neuroaccessespressoautomationtests.contacts

import android.os.Bundle
import android.os.SystemClock
import java.io.File
import android.util.Base64
import android.util.Log
import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ContactIdentityScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ContactsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ChatScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityDetailsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.NotificationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ViewIdentityScreen
import org.hamcrest.Matchers.allOf
import org.hamcrest.Matchers.equalTo
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith
import androidx.test.espresso.NoMatchingRootException
import androidx.test.espresso.NoMatchingViewException
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.ViewMatchers.withText
import org.hamcrest.Matchers.equalToIgnoringCase

/** Device phases coordinated by run-mutual-contacts-test.ps1; requires two distinct accounts. */
@RunWith(AndroidJUnit4::class)
class MutualContactsFlowTest : BaseTest() {
    /** Runs only the phase selected by the two-device runner, preserving account storage. */
    @Test
    fun runDevicePhase() {
        val arguments = InstrumentationRegistry.getArguments()
        assumeTrue("Use the mutualContactsTest task.", arguments.getString("contactsCoordinator") == "true")
        HomeScreen.returnToHome()
        when (arguments.getString("contactsPhase")) {
            "identify" -> {
                HomeScreen.openPersonalId(TestData.pin())
                ContactIdentityScreen.waitForText("Approved")
                val id = IdentityDetailsScreen.openAndReadNeuroId()
                result("contactIdentity", id)
            }
            "accept" -> NotificationsScreen.acceptIdentityRequest(TestData.pin())
            "export" -> {
                HomeScreen.openPersonalId(TestData.pin())
                val id = required("ownIdentity")
                ScreenWaiter.waitForLiveText("displayed_identity_$id", equalTo("Approved"))
                result("contactLink", ContactIdentityScreen.copyQrLink())
                Log.i("MutualContactsTest", "EXPORT: fresh QR clipboard link captured")
            }
            "sendPetition" -> {
                val expected = required("peerIdentity")
                check(expected != required("ownIdentity")) { "Both devices use the same identity." }
                openPeerLink()
                val petitionWasSent = ContactIdentityScreen.dismissPetitionSentDialogIfNeeded()
                result("petitionState", if (petitionWasSent) "sent" else "approved")
                Log.i("MutualContactsTest", "PETITION SENT: identity request delivered to the other device")
            }
            "addAccepted" -> {
                val expected = required("peerIdentity")
                check(expected != required("ownIdentity")) { "Both devices use the same identity." }
                openPeerLink()
                ViewIdentityScreen.assertDisplayed()
                ScreenWaiter.waitForLiveText("displayed_identity_$expected", equalTo("Approved"))

                val actual = IdentityDetailsScreen.openAndReadNeuroId()
                check(actual == expected) { "Scanned Neuro-ID differs from the exporting device." }
                Log.i("MutualContactsTest", "MATCH: scanned Neuro-ID matches the other device")
                ContactIdentityScreen.addContact()
                result("contactAdded", expected)
                Log.i("MutualContactsTest", "PASS: matching identity added; Remove contact is visible")
            }
            "verifyContact" -> {
                val expected = required("peerIdentity")
                ContactsScreen.verifySavedIdentity(expected)
                result("contactVerified", expected)
                Log.i("MutualContactsTest", "CONTACT VERIFIED: Apps / Contacts saved Neuro-ID matches peer")
            }
            "chatInitiator" -> exchangeMessages(initiator = true)
            "chatResponder" -> exchangeMessages(initiator = false)
            else -> error("Unknown contacts phase.")
        }
    }

    private fun openPeerLink() {
        ContactIdentityScreen.tapText("Scan QR")
        ContactIdentityScreen.tapText("Enter QR manually")
        ScreenWaiter.waitForText("Open")
        val link = String(Base64.decode(required("peerLink"), Base64.NO_WRAP), Charsets.UTF_8)
        onView(allOf(isAssignableFrom(EditText::class.java), isDisplayed()))
            .perform(replaceText(link), closeSoftKeyboard())
        ContactIdentityScreen.tapText("Open")
    }
    private fun exchangeMessages(initiator: Boolean) {
        val peer = required("peerIdentity")
        check(peer != required("ownIdentity")) { "Cannot message the same identity." }
        val runId = required("chatRunId")
        require(runId.matches(Regex("[a-f0-9]{32}"))) { "Expected a unique run identifier from the coordinator." }
        val fromA = "NeuroAccess $runId A to B"
        val fromB = "NeuroAccess $runId B to A"
        ContactsScreen.openChatFromHome(peer)
        if (initiator) {
            ChatScreen.send(fromA, peer)
            ChatScreen.awaitReceived(fromB, peer)
        } else {
            val release = File(InstrumentationRegistry.getInstrumentation().targetContext.cacheDir,
                "mutual-chat-$runId.release")
            check(!release.exists()) { "Chat release marker already exists for this run." }
            try {
                ChatScreen.awaitReceived(fromA, peer)
                ChatScreen.send(fromB, peer)
                // Keep the sender alive until A has actually received the reply.
                val deadline = SystemClock.elapsedRealtime() + 150_000
                while (!release.exists() && SystemClock.elapsedRealtime() < deadline) Thread.sleep(200)
                check(release.exists()) { "A did not confirm receipt of B's message." }
            } finally {
                release.delete()
            }
        }
        result("chatVerified", runId)
        Log.i("MutualContactsTest", "CHAT PASS: unique device message sent and peer message received ($runId)")
    }

    private fun required(name: String): String =
        requireNotNull(InstrumentationRegistry.getArguments().getString(name)?.takeIf { it.isNotBlank() }) {
            "Missing coordinator argument: $name"
        }

    private fun result(key: String, value: String) {
        InstrumentationRegistry.getInstrumentation().sendStatus(2, Bundle().apply {
            putString(key, Base64.encodeToString(value.toByteArray(Charsets.UTF_8), Base64.NO_WRAP))
        })
    }
}
