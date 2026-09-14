package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.os.SystemClock
import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.RootMatchers.withDecorView
import androidx.test.espresso.matcher.ViewMatchers.hasDescendant
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationIdOrAncestor
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf
import org.hamcrest.Matchers.startsWith

/** Enters PIN codes and verifies authentication outcomes. */
object PinAuthenticationPopup {
    /** Technical identifier of the PIN authentication popup. */
    const val POPUP = "popup_pin_authentication"
    private const val PIN = "input_authentication_pin"
    private const val CONFIRM = "button_authentication_pin_confirm"
    private const val INVALID_PIN_PREFIX = "PIN is invalid. You have "
    private const val REJECTION_TIMEOUT_MILLISECONDS = 15_000L

    /** Waits for the PIN authentication popup to appear. */
    fun assertDisplayed() {
        ScreenWaiter.waitFor(POPUP)
        onView(withAutomationId(POPUP)).check(matches(isDisplayed()))
    }

    /** Submits [pin] and waits for successful navigation to [nextScreen]. */
    fun enterPinAndWaitFor(pin: String, nextScreen: String) {
        this.enterPin(pin)
        if (nextScreen == ViewIdentityScreen.SCREEN) {
            ScreenWaiter.clickToLiveScreen(CONFIRM)
            ViewIdentityScreen.assertDisplayed()
        } else {
            ScreenWaiter.performActionAndWaitFor(nextScreen) {
                onView(withAutomationId(CONFIRM)).perform(click())
            }
        }
    }

    /** Submits [pin] once and verifies the rejection dialog in the English account flow. */
    fun enterPinAndExpectRejection(pin: String) {
        this.enterPin(pin)
        onView(withAutomationId(CONFIRM)).perform(click())
        this.waitForRejectionDialog()

        val rejectionMessage = allOf(
            withId(android.R.id.message),
            withText(startsWith(INVALID_PIN_PREFIX)),
            isDisplayed()
        )
        // MAUI's single-button DisplayAlert uses the negative Android button.
        onView(allOf(withId(android.R.id.button2), isDisplayed(), isEnabled()))
            .inRoot(allOf(isDialog(), withDecorView(hasDescendant(rejectionMessage))))
            .perform(click())

        this.assertDisplayed()
        onView(allOf(isAssignableFrom(EditText::class.java), withAutomationIdOrAncestor(PIN)))
            .check(matches(withText("")))
        check(!ScreenWaiter.isPresentInLayout(ViewIdentityScreen.SCREEN)) {
            "Identity must remain closed after a rejected PIN."
        }
    }

    private fun enterPin(pin: String) {
        onView(
            allOf(
                isAssignableFrom(EditText::class.java),
                withAutomationIdOrAncestor(PIN)
            )
        ).perform(click(), replaceText(pin), closeSoftKeyboard())
        ScreenWaiter.waitUntilReady(CONFIRM)
    }

    private fun waitForRejectionDialog() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.uptimeMillis() + REJECTION_TIMEOUT_MILLISECONDS
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            if (root != null) {
                val messages = root.findAccessibilityNodeInfosByText(INVALID_PIN_PREFIX)
                val rejected = try {
                    root.packageName?.toString() == instrumentation.targetContext.packageName &&
                        messages.any { message ->
                            message.isVisibleToUser &&
                                message.text?.toString()?.startsWith(INVALID_PIN_PREFIX) == true
                        }
                } finally {
                    @Suppress("DEPRECATION")
                    messages.forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    root.recycle()
                }
                if (rejected) {
                    return
                }
            }
            SystemClock.sleep(100L)
        } while (SystemClock.uptimeMillis() < deadline)

        error("The PIN rejection dialog did not appear within $REJECTION_TIMEOUT_MILLISECONDS ms.")
    }
}
