package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.content.res.Resources
import android.view.View
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import androidx.test.espresso.matcher.ViewMatchers.withResourceName
import org.hamcrest.Matcher
import org.hamcrest.Matchers.anyOf

object AutomationIdMatcher {

    fun withAutomationId(automationId: String): Matcher<View> {
        return anyOf(
            withContentDescription(automationId),
            withResourceName(automationId)
        )
    }

    fun matches(view: View, automationId: String): Boolean {
        if (view.contentDescription?.toString() == automationId) {
            return true
        }
        if (view.id == View.NO_ID) {
            return false
        }

        return try {
            view.resources.getResourceEntryName(view.id) == automationId
        } catch (_: Resources.NotFoundException) {
            false
        }
    }
}
