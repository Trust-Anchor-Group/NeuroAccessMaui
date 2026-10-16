package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.TextView
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.UiController
import androidx.test.espresso.ViewAction
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import org.hamcrest.Matcher
import org.hamcrest.Matchers.allOf
import org.hamcrest.Matchers.equalTo

/** Independent technical identity and visible-UI assertions with sanitized diagnostics. */
object UiContract {
    fun assertScreen(screenId: String) = checkControl(screenId)

    fun assertLabel(automationId: String, expectedText: String, screenId: String) =
        checkControl(automationId, equalTo(expectedText), screenId = screenId)

    /** Verifies separately rendered text inside an already identified composite region. */
    fun assertTextWithin(containerId: String, expectedText: String) =
        checkControl(containerId, equalTo(expectedText))

    /** Requires an exact independent label/state pairing for countdown controls. */
    fun assertActionStates(
        automationId: String, states: List<Pair<Matcher<String>, Boolean>>, screenId: String
    ) = inspect(automationId, screenId) { view ->
        val labels = descendants(view).filterIsInstance<TextView>()
            .filter { it !is EditText && isDisplayed().matches(it) }.toList()
        if (labels.size != 1) throw AssertionError("UI: action must expose one visible label.")
        val state = states.singleOrNull { it.first.matches(labels.single().text.toString()) }
            ?: throw AssertionError("UI: action text differs from the allowed localized states.")
        if (view.isEnabled != state.second) throw AssertionError("UI: action state differs from its text contract.")
    }

    /** Checks the real six-cell digit UI without exposing a verification code. */
    fun assertCodeDigits(automationId: String, screenId: String) = inspect(automationId, screenId) { view ->
        val digits = descendants(view).filterIsInstance<TextView>()
            .filter { it !is EditText && isDisplayed().matches(it) }.toList()
        if (digits.size != 6 || !view.isEnabled) {
            throw AssertionError("UI: verification control must have six enabled visible digit cells.")
        }
    }

    fun assertAction(automationId: String, expectedText: String, enabled: Boolean, screenId: String) =
        checkControl(automationId, equalTo(expectedText), enabled, screenId)

    /** Uses independently specified templates for genuinely dynamic labels, not observed-text fallback. */
    fun assertAction(automationId: String, expectedText: Matcher<String>, enabled: Boolean, screenId: String) =
        checkControl(automationId, expectedText, enabled, screenId)

    fun assertIcon(automationId: String, enabled: Boolean, screenId: String) =
        checkControl(automationId, enabled = enabled, screenId = screenId)

    fun assertImage(automationId: String, screenId: String) = inspect(automationId, screenId) { view ->
        val images = descendants(view).filterIsInstance<android.widget.ImageView>()
            .filter { isDisplayed().matches(it) }.toList()
        if (images.size != 1) throw AssertionError("TECHNICAL: expected one image within the identified control.")
    }

    fun assertInput(automationId: String, enabled: Boolean, screenId: String, expectedValue: String? = null) {
        checkControl(automationId, enabled = enabled, screenId = screenId)
        inspect(automationId, screenId) { view ->
            val inputs = descendants(view).filterIsInstance<EditText>().filter { isDisplayed().matches(it) }.toList()
            if (inputs.size != 1) throw AssertionError("TECHNICAL: expected one native input in identified control.")
            if (expectedValue != null && inputs.single().text.toString() != expectedValue) {
                throw AssertionError("UI: configured input value differs.")
            }
        }
    }

    /** Enters configured data without including the value in Espresso action descriptions or causes. */
    fun enterText(automationId: String, value: String, screenId: String) {
        assertInput(automationId, enabled = true, screenId = screenId)
        val matcher = allOf(isAssignableFrom(EditText::class.java),
            AutomationIdMatcher.withAutomationIdOrAncestor(automationId),
            androidx.test.espresso.matcher.ViewMatchers.isDescendantOfA(
                AutomationIdMatcher.withAutomationId(screenId)), isDisplayed())
        val delegate = replaceText(value)
        try {
            onView(matcher).perform(object : ViewAction {
                override fun getConstraints(): Matcher<View> = delegate.constraints
                override fun getDescription(): String = "Enter configured input (redacted)"
                override fun perform(uiController: UiController, view: View) = delegate.perform(uiController, view)
            }, closeSoftKeyboard())
        } catch (_: Throwable) {
            throw AssertionError("TECHNICAL: could not enter configured input into identified control.")
        }
        assertInput(automationId, enabled = true, screenId = screenId, expectedValue = value)
    }

    /** Reads uniquely identified displayed data or locale bootstrap; never defines expected UI text. */
    fun readVisibleLabel(automationId: String, screenId: String? = null): String {
        var label: String? = null
        inspect(automationId, screenId) { view ->
            val labels = descendants(view).filterIsInstance<TextView>()
                .filter { isDisplayed().matches(it) && it.text.isNotEmpty() && it !is EditText }.toList()
            if (labels.size != 1) throw AssertionError("UI: expected one visible label.")
            label = labels.single().text.toString()
        }
        return checkNotNull(label)
    }

    private fun checkControl(
        id: String, text: Matcher<String>? = null, enabled: Boolean? = null,
        screenId: String? = null, nativeType: Class<out View>? = null
    ) = inspect(id, screenId) { view ->
        if (nativeType != null && !nativeType.isInstance(view)) {
            throw AssertionError("TECHNICAL: wrong native control type.")
        }
        if (enabled != null && view.isEnabled != enabled) {
            throw AssertionError("UI: enabled state differs from its contract.")
        }
        if (text != null) {
            val count = descendants(view).filterIsInstance<TextView>().count {
                it !is EditText && isDisplayed().matches(it) && text.matches(it.text.toString())
            }
            if (count != 1) throw AssertionError("UI: exact visible label differs from its contract.")
        }
    }

    private fun inspect(id: String, screenId: String?, assertion: (View) -> Unit) {
        if (screenId != null) assertScreen(screenId)
        val target = allOf(AutomationIdMatcher.withAutomationId(id, screenId), isDisplayed())
        val description = ScreenWaiter.diagnosticId(id)
        try {
            onView(target).check { view, missing ->
                if (missing != null || view == null) throw AssertionError("TECHNICAL: missing displayed control.")
                assertion(view)
            }
        } catch (failure: AssertionError) {
            val message = failure.message.orEmpty()
            if (message.startsWith("UI:") || message.startsWith("TECHNICAL:")) {
                throw AssertionError("$message Control: '$description'.")
            }
            throw AssertionError("TECHNICAL: contract assertion failed for '$description'.")
        } catch (_: androidx.test.espresso.AmbiguousViewMatcherException) {
            throw AssertionError("TECHNICAL: duplicate displayed AutomationId '$description'.")
        } catch (_: Throwable) {
            throw AssertionError("TECHNICAL: unable to inspect AutomationId '$description'.")
        }
    }

    private fun descendants(view: View): Sequence<View> = sequence {
        yield(view)
        if (view is ViewGroup) {
            for (index in 0 until view.childCount) yieldAll(descendants(view.getChildAt(index)))
        }
    }
}
