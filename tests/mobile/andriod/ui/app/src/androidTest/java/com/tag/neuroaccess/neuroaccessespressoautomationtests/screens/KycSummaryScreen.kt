package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.UiController
import androidx.test.espresso.ViewAction
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestData
import java.time.format.DateTimeFormatter
import java.util.Locale
import org.hamcrest.Matcher

/** Verifies mapped summary values before the English personal ID application is submitted. */
object KycSummaryScreen {
    private const val MAX_SUMMARY_SCROLLS = 48

    /** Checks the entered [data], including middle name when [includesMiddleName] is true. */
    fun assertMatches(data: PersonalIdentityTestData, includesMiddleName: Boolean) {
        ScreenWaiter.waitFor(KycProcessScreen.SUMMARY_SCREEN)
        val pending = this.expectedValues(data, includesMiddleName).toMutableMap()
        var scrolls = 0
        while (true) {
            this.checkVisibleValues(pending)
            if (pending.isEmpty() || scrolls == MAX_SUMMARY_SCROLLS) {
                break
            }
            onView(withAutomationId(KycProcessScreen.SUMMARY_SCREEN))
                .perform(this.scrollSummary())
            scrolls++
        }
        check(pending.isEmpty()) {
            "KYC summary is missing visible values for mappings: ${pending.keys.joinToString()}."
        }

        // Return to the header so the submission result can be seen without further scrolling.
        onView(withAutomationId(KycProcessScreen.SUMMARY_SCREEN))
            .perform(this.scrollSummary(toTop = true))
    }

    private fun scrollSummary(toTop: Boolean = false): ViewAction = object : ViewAction {
        override fun getConstraints(): Matcher<View> = isDisplayed()

        override fun getDescription(): String =
            if (toTop) "scroll KYC summary to its header" else "scroll KYC summary by half a viewport"

        override fun perform(uiController: UiController, view: View) {
            // Use the outer list; swipes can fling past rows or target a nested collection.
            val collection = checkNotNull(descendants(view).filterIsInstance<RecyclerView>().firstOrNull()) {
                "KYC summary collection was not found inside its identified layout."
            }
            collection.stopScroll()
            if (toTop) {
                val layout = collection.layoutManager as? LinearLayoutManager
                    ?: error("KYC summary requires a linear layout manager.")
                layout.scrollToPositionWithOffset(0, 0)
            } else {
                val viewportHeight = collection.height - collection.paddingTop - collection.paddingBottom
                check(viewportHeight > 0) { "KYC summary has no scrollable viewport." }
                collection.scrollBy(0, (viewportHeight / 2).coerceAtLeast(1))
            }
            uiController.loopMainThreadUntilIdle()
        }
    }

    private fun expectedValues(
        data: PersonalIdentityTestData,
        includesMiddleName: Boolean
    ): Map<String, Set<String>> {
        val expected = linkedMapOf(
            "FIRST" to setOf(data.firstName),
            "LAST" to setOf(data.lastName),
            "PNR" to setOf(data.personalNumber),
            "NATIONALITY" to setOf(data.nationality),
            "GENDER" to setOf(data.gender.displayName),
            "ADDR" to setOf(data.address),
            "ADDR2" to setOf(data.apartment),
            "AREA" to setOf(data.area),
            "CITY" to setOf(data.city),
            "ZIP" to setOf(data.zipCode),
            "REGION" to setOf(data.region)
        )
        if (includesMiddleName) {
            expected["MIDDLE"] = setOf(data.middleName)
        }
        // .NET's long English date can use month-first or day-first ordering by culture.
        expected["BDATE"] = listOf(
            "EEEE, MMMM d, yyyy",
            "EEEE, d MMMM yyyy",
            "EEEE, dd MMMM yyyy",
            "EEEE d MMMM yyyy"
        ).map { pattern ->
            data.dateOfBirth.format(DateTimeFormatter.ofPattern(pattern, Locale.ENGLISH))
        }.toSet()
        return expected
    }

    private fun checkVisibleValues(pending: MutableMap<String, Set<String>>) {
        onView(withAutomationId(KycProcessScreen.SUMMARY_SCREEN)).check { root, missingView ->
            if (missingView != null) {
                throw missingView
            }
            val labels = this.descendants(checkNotNull(root))
                .filterIsInstance<TextView>()
                .filter { label -> isDisplayed().matches(label) }
                .toList()
            val entries = pending.entries.iterator()
            while (entries.hasNext()) {
                val (mapping, expected) = entries.next()
                val matcher = withAutomationId("value_kyc_summary_$mapping")
                val matches = labels.filter { label -> matcher.matches(label) }
                check(matches.size <= 1) { "KYC summary has duplicate visible values for $mapping." }
                if (matches.isEmpty()) {
                    continue
                }
                check(matches.single().text.toString() in expected) {
                    "KYC summary value differs from entered data for mapping $mapping."
                }
                entries.remove()
            }
        }
    }

    private fun descendants(view: View): Sequence<View> = sequence {
        yield(view)
        if (view is ViewGroup) {
            for (index in 0 until view.childCount) {
                yieldAll(descendants(view.getChildAt(index)))
            }
        }
    }
}
