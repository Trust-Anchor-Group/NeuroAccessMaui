package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared

import android.os.SystemClock
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.RootMatchers.withDecorView
import androidx.test.espresso.matcher.ViewMatchers.hasDescendant
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.ViewIdentityScreen
import org.hamcrest.Matchers.allOf

/** Uses the real authentication popup and independently localized rejection contract. */
object PinAuthenticationPopup {
    const val POPUP = UiIds.PopupSecurityPassword
    private const val PIN = UiIds.InputSecurityPasswordValue
    private const val CONFIRM = UiIds.ButtonSecurityPasswordEnter

    fun assertDisplayed() {
        ScreenWaiter.waitFor(POPUP)
        UiContract.assertScreen(POPUP)
        UiContract.assertLabel(UiIds.LabelSecurityPasswordTitle, UiText.text("EnterPassword"), POPUP)
        UiContract.assertInput(PIN, enabled = true, screenId = POPUP, expectedValue = "")
        UiContract.assertAction(CONFIRM, UiText.text("Enter"), enabled = false, screenId = POPUP)
        UiContract.assertAction(UiIds.ButtonSecurityPasswordCancel, UiText.text("Cancel"),
            enabled = true, screenId = POPUP)
    }

    fun enterPinAndWaitFor(pin: String, nextScreen: String) {
        enterPin(pin)
        ScreenWaiter.clickToLiveScreen(CONFIRM, POPUP)
        ScreenWaiter.waitUntilHidden(POPUP)
        ScreenWaiter.waitForAnyLive(nextScreen)
    }

    fun enterPinAndExpectRejection(pin: String) {
        enterPin(pin)
        ScreenWaiter.clickToLiveScreen(CONFIRM, POPUP)
        waitForRejectionDialog()
        val message = allOf(withId(android.R.id.message),
            withText(UiText.numericTemplate("PasswordIsInvalid")), isDisplayed())
        try {
            onView(allOf(withId(android.R.id.button2), isDisplayed(), isEnabled(),
                withText(UiText.text("Ok"))))
                .inRoot(allOf(isDialog(), withDecorView(hasDescendant(message))))
                .perform(click())
        } catch (_: Throwable) {
            throw AssertionError("UI: authentication rejection dialog differs from its contract.")
        }
        assertDisplayed()
        check(!ScreenWaiter.isPresentInLayout(ViewIdentityScreen.SCREEN)) {
            "FLOW: identity opened after a rejected credential."
        }
    }

    private fun enterPin(pin: String) {
        UiContract.enterText(PIN, pin, POPUP)
        ScreenWaiter.waitUntilReady(CONFIRM, POPUP)
        UiContract.assertAction(CONFIRM, UiText.text("Enter"), enabled = true, screenId = POPUP)
    }

    private fun waitForRejectionDialog() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.uptimeMillis() + 15_000L
        val matcher = UiText.numericTemplate("PasswordIsInvalid")
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            if (root != null) {
                val messages = root.findAccessibilityNodeInfosByViewId(
                    "${instrumentation.targetContext.packageName}:id/message")
                // Android alerts can expose framework android:id/message rather than app ids.
                val nativeMessages = root.findAccessibilityNodeInfosByViewId("android:id/message")
                val titles = root.findAccessibilityNodeInfosByText(UiText.text("ErrorTitle"))
                val rejected = try {
                    (messages + nativeMessages).distinct().count {
                        it.isVisibleToUser && matcher.matches(it.text?.toString())
                    } == 1 && titles.count {
                        it.isVisibleToUser && it.text?.toString() == UiText.text("ErrorTitle")
                    } == 1
                } finally {
                    @Suppress("DEPRECATION")
                    (messages + nativeMessages + titles).distinct().forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    root.recycle()
                }
                if (rejected) return
            }
            SystemClock.sleep(100L)
        } while (SystemClock.uptimeMillis() < deadline)
        throw AssertionError("UI: expected one localized authentication rejection dialog.")
    }
}
