package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.content.res.Resources
import android.view.View
import androidx.test.espresso.matcher.ViewMatchers.isDescendantOfA
import org.hamcrest.Description
import org.hamcrest.Matcher
import org.hamcrest.TypeSafeMatcher
import org.hamcrest.Matchers.anyOf

/** Matches technical MAUI identifiers independently of spoken accessibility descriptions. */
object AutomationIdMatcher {

    /** Returns a matcher for the native resource ID or MAUI accessibility-node resource ID. */
    fun withAutomationId(automationId: String): Matcher<View> = object : TypeSafeMatcher<View>() {
        override fun describeTo(description: Description) {
            description.appendText("view with automation ID ").appendValue(automationId)
        }

        override fun matchesSafely(view: View): Boolean =
            AutomationIdMatcher.matches(view, automationId)
    }

    /** Matches an input itself or an input nested inside an identified composite control. */
    fun withAutomationIdOrAncestor(automationId: String): Matcher<View> = anyOf(
        withAutomationId(automationId),
        isDescendantOfA(withAutomationId(automationId))
    )

    /** Reads the same technical identifiers used by [withAutomationId] for screen waits. */
    fun matches(view: View, automationId: String): Boolean {
        if (view.id != View.NO_ID) {
            try {
                if (view.resources.getResourceEntryName(view.id) == automationId) {
                    return true
                }
            } catch (_: Resources.NotFoundException) {
                // MAUI can generate view IDs without a compiled Android resource entry.
            }
        }

        // MAUI publishes this ID through its delegate, not Resources.getResourceEntryName.
        val node = view.createAccessibilityNodeInfo() ?: return false
        return try {
            node.viewIdResourceName == "${view.context.packageName}:id/$automationId"
        } finally {
            @Suppress("DEPRECATION")
            node.recycle()
        }
    }
}
