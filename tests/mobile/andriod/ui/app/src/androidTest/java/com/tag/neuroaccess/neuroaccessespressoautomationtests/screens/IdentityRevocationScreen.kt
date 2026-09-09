package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.os.SystemClock
import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.Espresso.pressBack
import androidx.test.espresso.action.ViewActions.click
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
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationIdOrAncestor
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.TestOtpClient
import org.hamcrest.Matchers.allOf

object IdentityRevocationScreen {
    private const val MESSAGE = "Are you sure you want to revoke your identity from the application?"

    fun revokeAndReverify(identityId: String) {
        ApplicationsScreen.assertDisplayed()
        pressBack()
        HomeScreen.assertDisplayed()
        HomeScreen.openSettings()
        SettingsScreen.assertDisplayed()
        SettingsScreen.requestIdentityRevocation()
        waitForConfirmation()
        onView(allOf(withId(android.R.id.button1), isDisplayed(), isEnabled()))
            .inRoot(allOf(isDialog(), withDecorView(hasDescendant(
                allOf(withId(android.R.id.message), withText(MESSAGE))
            ))))
            .perform(click())
        PinAuthenticationPopup.assertDisplayed()
        PinAuthenticationPopup.enterPinAndWaitFor(TestData.pin(), PhoneVerificationScreen.SCREEN)

        // Navigation alone also occurs when revocation is forbidden and the local identity is cleared.
        // Require the same identity and the state returned by ObsoleteLegalIdentity instead.
        ScreenWaiter.waitFor("identity_profile_Obsoleted_$identityId")
        reverifyPhone()
    }

    private fun reverifyPhone() {
        repeat(2) {
            if (ScreenWaiter.waitForAnyReady("button_continue_success", "button_send_phone_code") ==
                "button_continue_success") {
                SuccessScreen.continueToHome()
                HomeScreen.assertDisplayed()
                return
            }
            PhoneVerificationScreen.assertDisplayed()
            onView(allOf(isAssignableFrom(EditText::class.java),
                withAutomationIdOrAncestor("input_phone_number")))
                .check(matches(withText(TestData.phoneNumber())))
            var nextScreen = PhoneVerificationScreen.sendCodeAfterRevocation()
            if (nextScreen == PhoneCodeVerificationScreen.SCREEN) {
                PhoneCodeVerificationScreen.assertDisplayed()
                val code = TestOtpClient.fetchVerificationCode("+1" + TestData.phoneNumber())
                PhoneCodeVerificationScreen.enterVerificationCode(code)
                nextScreen = PhoneCodeVerificationScreen.verifyCodeAfterRevocation()
            }
            if (nextScreen == SuccessScreen.SCREEN) {
                SuccessScreen.continueToHome()
                HomeScreen.assertDisplayed()
                return
            }
            // CreateAccount clears an obsolete profile and can return to ValidatePhone once.
        }
        error("Phone reverification returned to the phone step twice instead of completing onboarding.")
    }

    private fun waitForConfirmation() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.uptimeMillis() + 30_000L
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            if (root != null) {
                val messages = root.findAccessibilityNodeInfosByText(MESSAGE)
                val found = try {
                    root.packageName?.toString() == instrumentation.targetContext.packageName &&
                        messages.any { it.isVisibleToUser && it.text?.toString() == MESSAGE }
                } finally {
                    @Suppress("DEPRECATION")
                    messages.forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    root.recycle()
                }
                if (found) return
            }
            SystemClock.sleep(250L)
        } while (SystemClock.uptimeMillis() < deadline)
        error("The English identity revocation confirmation did not appear.")
    }
}
