package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData

import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.QuickLoginClient
import org.hamcrest.Matchers.allOf

/** Opens QR payloads through the scanner's manual entry UI without scanning an image. */
object ScanQrScreen {
    /**
     * Switches the already open scanner to manual entry, then requests and opens a fresh login.
     * Camera permission must already be handled by the calling flow.
     * @param nextScreen Automation ID expected after opening the link, such as the signing screen.
     * @param endpoint Quick Login API endpoint.
     * @param tabId Optional TabID of the browser page receiving the login result.
     * @param manualEntryLabel Localized label of the scanner's manual entry button.
     * @param openLabel Localized label of the manual entry submit button.
     */
    fun openQuickLogin(
        nextScreen: String,
        endpoint: String = TestData.quickLoginEndpoint(),
        tabId: String = "",
        manualEntryLabel: String = "Enter QR manually",
        openLabel: String = "Open"
    ) {
        require(nextScreen.isNotBlank()) { "An expected destination screen is required." }
        onView(allOf(withText(manualEntryLabel), isDisplayed())).perform(click())
        val signUrl = QuickLoginClient.fetchSignUrl(endpoint = endpoint, tabId = tabId)
        this.enterLinkAndOpen(signUrl, nextScreen, openLabel)
    }

    /**
     * Opens a QR link from the visible manual entry form and waits for the expected destination.
     * @param signUrl Fresh QR payload from QuickLoginClient or the current page's QR link.
     * @param nextScreen Automation ID expected after submitting the payload.
     * @param openLabel Localized label of the submit button.
     */
    fun enterLinkAndOpen(signUrl: String, nextScreen: String, openLabel: String = "Open") {
        require(signUrl.isNotBlank()) { "A QR payload is required." }
        require(nextScreen.isNotBlank()) { "An expected destination screen is required." }
        onView(allOf(isAssignableFrom(EditText::class.java), isDisplayed()))
            .perform(replaceText(signUrl), closeSoftKeyboard())
        ScreenWaiter.performActionAndWaitFor(nextScreen) {
            onView(allOf(withText(openLabel), isDisplayed())).perform(click())
        }
    }
}
