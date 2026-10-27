package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import android.view.View
import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.AmbiguousViewMatcherException
import androidx.test.espresso.NoMatchingViewException
import androidx.test.espresso.PerformException
import androidx.test.espresso.UiController
import androidx.test.espresso.ViewAction
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.matcher.ViewMatchers.hasFocus
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import org.hamcrest.Matcher
import org.hamcrest.Matchers.allOf

/** Uses the real visible digit control and the focused hidden native input behind it. */
object VerificationCodeActions {
    /** Types a six-digit code without changing the production control's opacity or behavior. */
    fun enter(code: String) {
        require(code.matches(Regex("[0-9]{6}"))) { "Verification code must contain six digits." }
        UiContract.assertCodeDigits(UiIds.ControlSecurityVerifycodeDigits, UiIds.ScreenSecurityVerifycode)
        try {
            onView(AutomationIdMatcher.withAutomationId(UiIds.ControlSecurityVerifycodeDigits, UiIds.ScreenSecurityVerifycode)).perform(click())
            onView(AutomationIdMatcher.withAutomationId(UiIds.InputSecurityVerifycodeValue, UiIds.ScreenSecurityVerifycode))
                .perform(object : ViewAction {
                    override fun getDescription(): String = "type a verification code into its focused input"

                    override fun getConstraints(): Matcher<View> = allOf(
                        isAssignableFrom(EditText::class.java), hasFocus(), isEnabled()
                    )

                    override fun perform(uiController: UiController, view: View) {
                        check((view as EditText).text.isEmpty()) { "Verification input must be empty before typing." }
                        check(uiController.injectString(code)) { "Could not type verification code." }
                        uiController.loopMainThreadUntilIdle()
                    }
                }, closeSoftKeyboard())
        } catch (_: PerformException) {
            // Espresso's detailed view dump can contain a verification code; do not forward it.
            throw AssertionError("UI: verification-code input did not accept typing.")
        } catch (_: NoMatchingViewException) {
            throw AssertionError("TECHNICAL: missing verification-code input or digit control.")
        } catch (_: AmbiguousViewMatcherException) {
            throw AssertionError("TECHNICAL: duplicate verification-code input or digit control.")
        }
    }
}
