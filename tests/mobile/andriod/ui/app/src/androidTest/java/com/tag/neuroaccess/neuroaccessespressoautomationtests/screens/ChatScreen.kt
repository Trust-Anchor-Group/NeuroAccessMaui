package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.util.Log
import android.widget.EditText
import android.widget.TextView
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDescendantOfA
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationIdOrAncestor
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScrollableScreen
import org.hamcrest.Matchers.allOf

/** Sends through the chat UI and distinguishes incoming message bubbles from local sent text. */
object ChatScreen {
    private const val SCREEN = "screen_chat"
    private const val INPUT = "input_chat_message"

    /** Checks that the opened chat belongs to the exact legal identity selected in Contacts. */
    fun assertPeer(peerIdentity: String) {
        ScreenWaiter.waitForAnyLive(SCREEN)
        ScreenWaiter.waitForAnyLive("chat_peer_$peerIdentity")
    }

    /** Sends the unique device message through the editor and the real Send command. */
    fun send(message: String, peerIdentity: String) {
        assertPeer(peerIdentity)
        onView(allOf(isAssignableFrom(EditText::class.java), withAutomationIdOrAncestor(INPUT), isDisplayed()))
            .perform(replaceText(message), closeSoftKeyboard())
        ScreenWaiter.clickToLiveScreen("button_chat_send")
        waitForBubble(message, "chat_messages_sent", 30_000)
        Log.i("MutualContactsTest", "SENT: $message")
    }

    /** Requires the exact run-specific message inside an incoming bubble on the peer's chat. */
    fun awaitReceived(message: String, peerIdentity: String) {
        assertPeer(peerIdentity)
        waitForBubble(message, "chat_messages_received", 120_000)
        Log.i("MutualContactsTest", "RECEIVED: $message")
    }

    private fun waitForBubble(message: String, frameId: String, timeoutMillis: Long) {
        ScrollableScreen.waitForVisibleMatch(SCREEN, "scroll_chat_messages", "$frameId containing $message", timeoutMillis) {
            it is TextView && it !is EditText && it.text.toString().trim() == message &&
                isDescendantOfA(withAutomationId(frameId)).matches(it)
        }
    }
}
