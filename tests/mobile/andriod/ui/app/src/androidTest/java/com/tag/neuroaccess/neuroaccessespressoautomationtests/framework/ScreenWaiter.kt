package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.os.Handler
import android.os.Looper
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.test.espresso.Espresso.onIdle
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.IdlingPolicies
import androidx.test.espresso.IdlingRegistry
import androidx.test.espresso.IdlingResource
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed as espressoIsDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isDisplayingAtLeast
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import org.hamcrest.Matcher
import org.hamcrest.Matchers.allOf
import java.util.concurrent.TimeUnit

/** Observes fresh active roots without selecting arbitrary matches or exposing entered values. */
object ScreenWaiter {
    private const val TIMEOUT_SECONDS = 120L
    private const val LIVE_TIMEOUT_MILLISECONDS = 15_000L
    private const val CLICK_VISIBLE_PERCENTAGE = 90

    init {
        IdlingPolicies.setIdlingResourceTimeout(TIMEOUT_SECONDS + 5L, TimeUnit.SECONDS)
        IdlingPolicies.setMasterPolicyTimeout(TIMEOUT_SECONDS + 10L, TimeUnit.SECONDS)
    }

    /** Hides opaque instance keys from failure and idling-resource descriptions. */
    fun diagnosticId(automationId: String): String =
        if ("_key_" in automationId) automationId.substringBefore("_key_") + "_key_<redacted>"
        else automationId

    /** Injects one tap only after a unique identified control is ready in the requested root. */
    fun clickToLiveScreen(automationId: String, screenId: String? = null) {
        val identity = AutomationIdMatcher.withAutomationId(automationId, screenId)
        var lastCount = 0
        var scopeCount = 1
        var stableObservations = 0
        var candidate: View? = null
        awaitLiveCondition(
            "TECHNICAL: control ${diagnosticId(automationId)}",
            failure = { if (scopeCount != 1 && screenId != null) identityFailure(screenId, scopeCount)
                else if (lastCount == 1) AssertionError("UI: identified control is not ready for interaction.")
                else identityFailure(automationId, lastCount) }
        ) {
            onMainThread {
                val roots = resumedRoots()
                scopeCount = if (screenId == null) 1 else matchingViews(roots,
                    allOf(AutomationIdMatcher.withAutomationId(screenId), espressoIsDisplayed())).size
                val targets = matchingViews(roots, allOf(identity, espressoIsDisplayed()))
                lastCount = targets.size
                val unique = targets.singleOrNull()
                val ready = scopeCount == 1 && unique != null && isEnabled().matches(unique) &&
                    isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(unique)
                stableObservations = if (ready && unique === candidate) stableObservations + 1 else 0
                candidate = if (ready) unique else null
                stableObservations >= 2
            }
        }
        val target = onMainThread {
            val roots = resumedRoots()
            if (screenId != null) {
                val count = matchingViews(roots,
                    allOf(AutomationIdMatcher.withAutomationId(screenId), espressoIsDisplayed())).size
                if (count != 1) throw identityFailure(screenId, count)
            }
            val targets = matchingViews(roots, allOf(identity, espressoIsDisplayed()))
            val unique = targets.singleOrNull()
            if (unique == null || unique !== candidate) throw identityFailure(automationId, targets.size)
            if (!unique.isEnabled || !isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(unique)) {
                throw AssertionError("UI: control is no longer ready for interaction.")
            }
            val bounds = android.graphics.Rect()
            check(unique.getGlobalVisibleRect(bounds)) { "UI: no visible interaction bounds." }
            android.graphics.Point(bounds.centerX(), bounds.centerY())
        }
        val automation = InstrumentationRegistry.getInstrumentation().uiAutomation
        val downTime = android.os.SystemClock.uptimeMillis()
        fun inject(action: Int) {
            val event = android.view.MotionEvent.obtain(downTime,
                android.os.SystemClock.uptimeMillis(), action, target.x.toFloat(), target.y.toFloat(), 0)
            event.source = android.view.InputDevice.SOURCE_TOUCHSCREEN
            try {
                check(automation.injectInputEvent(event, true)) { "TECHNICAL: could not inject interaction." }
            } finally {
                event.recycle()
            }
        }
        inject(android.view.MotionEvent.ACTION_DOWN)
        inject(android.view.MotionEvent.ACTION_UP)
    }

    /** Waits through transient overlap; exactly one requested destination must remain visible. */
    fun waitForAnyLive(vararg automationIds: String): String {
        requireCandidates(automationIds)
        var selected: String? = null
        var observations = 0
        awaitLiveCondition("TECHNICAL: one unambiguous destination",
            failure = { onMainThread { candidateFailure(automationIds) } }) {
            val current = onMainThread { uniqueCandidate(resumedRoots(), automationIds, ready = false) }
            observations = if (current != null && current == selected) observations + 1 else 0
            selected = current
            observations >= 2
        }
        return checkNotNull(selected)
    }

    /** Checks text only after technical identity is independently unique. */
    fun waitForLiveText(automationId: String, expectedText: Matcher<String>, screenId: String? = null) {
        val identity = allOf(AutomationIdMatcher.withAutomationId(automationId, screenId), espressoIsDisplayed())
        var lastCount = 0
        awaitLiveCondition("UI: identified control satisfying the text contract",
            failure = { if (lastCount == 1) AssertionError("UI: text contract not satisfied.")
                else identityFailure(automationId, lastCount) }) {
            onMainThread {
                val targets = matchingViews(resumedRoots(), identity)
                lastCount = targets.size
                targets.singleOrNull()?.let { withText(expectedText).matches(it) } == true
            }
        }
    }

    /** Waits for exactly one displayed identified view in the selected root. */
    fun waitFor(screenAutomationId: String, screenId: String? = null) =
        waitForIdentified(screenAutomationId, screenId)

    /** Native/legacy text observation; text must be unique and is never an ID fallback. */
    fun waitForText(expectedText: String) =
        waitForMatcher("UI: visible text contract", allOf(withText(expectedText), espressoIsDisplayed()))

    fun waitForText(expectedText: Matcher<String>, timeoutMilliseconds: Long) {
        awaitLiveCondition("UI: visible text contract", timeoutMilliseconds) {
            onMainThread {
                matchingViews(resumedRoots(), allOf(withText(expectedText), espressoIsDisplayed())).size == 1
            }
        }
    }

    fun waitForText(automationId: String, expectedText: Matcher<String>) =
        waitForIdentified(automationId, condition = withText(expectedText))

    fun waitUntilEnabled(automationId: String, screenId: String? = null) =
        waitForIdentified(automationId, screenId, isEnabled())

    fun waitUntilReady(automationId: String, screenId: String? = null) =
        waitForIdentified(automationId, screenId,
            allOf(isEnabled(), isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE)))

    fun waitForAny(vararg screenAutomationIds: String): String = waitForCandidates(screenAutomationIds, false)

    fun waitForAnyReady(vararg automationIds: String): String = waitForCandidates(automationIds, true)

    fun waitForIdle() = onIdle()

    /** Rejects duplicate shown instances even if one lies outside the viewport. */
    fun isPresentInLayout(automationId: String, screenId: String? = null): Boolean =
        uniquePresence(automationId, screenId) { it.isShown }

    fun isDisplayed(automationId: String, screenId: String? = null): Boolean =
        uniquePresence(automationId, screenId) { espressoIsDisplayed().matches(it) }

    fun isDisplayedOnScreen(automationId: String, screenId: String? = null): Boolean =
        uniquePresence(automationId, screenId) { isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(it) }

    /** Legacy observation requires uniqueness; never returns an arbitrary first label. */
    fun firstDisplayedEnabledText(vararg expectedTexts: String): String? = onMainThread {
        val expected = expectedTexts.toSet()
        val targets = resumedRoots().flatMap { descendants(it).toList() }.filter { view ->
            view is TextView && view.text.toString() in expected &&
                isEnabled().matches(view) && isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(view)
        }
        if (targets.size > 1) throw AssertionError("TECHNICAL: ambiguous visible text observation.")
        (targets.singleOrNull() as? TextView)?.text?.toString()
    }

    fun waitUntilHidden(automationId: String) {
        val identity = allOf(AutomationIdMatcher.withAutomationId(automationId), espressoIsDisplayed())
        awaitCondition("TECHNICAL: hidden ${diagnosticId(automationId)}") { roots ->
            matchingViews(roots, identity).isEmpty()
        }
    }

    fun performActionAndWaitFor(screenAutomationId: String, action: () -> Unit) {
        action()
        waitFor(screenAutomationId)
    }

    private fun waitForIdentified(
        automationId: String, screenId: String? = null, condition: Matcher<View>? = null
    ) {
        val identity = allOf(AutomationIdMatcher.withAutomationId(automationId, screenId), espressoIsDisplayed())
        var lastCount = 0
        var scopeCount = 1
        var observations = 0
        var candidate: View? = null
        awaitCondition("TECHNICAL: unique ${diagnosticId(automationId)}",
            failure = { if (scopeCount != 1 && screenId != null) identityFailure(screenId, scopeCount)
                else if (lastCount == 1) AssertionError("UI: identified control did not become ready.")
                else identityFailure(automationId, lastCount) }) { roots ->
            scopeCount = if (screenId == null) 1 else matchingViews(roots,
                allOf(AutomationIdMatcher.withAutomationId(screenId), espressoIsDisplayed())).size
            val targets = matchingViews(roots, identity)
            lastCount = targets.size
            val unique = targets.singleOrNull()
            val satisfied = scopeCount == 1 && unique != null && (condition == null || condition.matches(unique))
            observations = if (satisfied && unique === candidate) observations + 1 else 0
            candidate = if (satisfied) unique else null
            observations >= 2
        }
        try {
            onView(identity).check(matches(if (condition == null) identity else allOf(identity, condition)))
        } catch (_: Throwable) {
            throw AssertionError("TECHNICAL: identified control changed after the wait.")
        }
    }

    private fun waitForCandidates(ids: Array<out String>, ready: Boolean): String {
        requireCandidates(ids)
        var selected: String? = null
        var observations = 0
        awaitCondition("TECHNICAL: one unambiguous destination",
            failure = { onMainThread { candidateFailure(ids) } }) { roots ->
            val current = uniqueCandidate(roots, ids, ready)
            observations = if (current != null && current == selected) observations + 1 else 0
            selected = current
            observations >= 2
        }
        return checkNotNull(selected)
    }

    private fun candidateFailure(ids: Array<out String>): AssertionError {
        val counts = ids.map { id -> matchingViews(resumedRoots(),
            allOf(AutomationIdMatcher.withAutomationId(id), espressoIsDisplayed())).size }
        return AssertionError(when {
            counts.any { it > 1 } -> "TECHNICAL: duplicate AutomationId among the requested destinations."
            counts.count { it == 1 } > 1 -> "TECHNICAL: multiple requested destinations remain active."
            counts.all { it == 0 } -> "TECHNICAL: missing all requested destination IDs."
            else -> "UI: destination did not become stably ready."
        })
    }

    private fun requireCandidates(ids: Array<out String>) {
        require(ids.isNotEmpty() && ids.distinct().size == ids.size) {
            "CONTRACT: distinct candidate identifiers are required."
        }
    }

    private fun uniqueCandidate(roots: List<View>, ids: Array<out String>, ready: Boolean): String? {
        val candidates = ids.map { id ->
            id to matchingViews(roots, allOf(AutomationIdMatcher.withAutomationId(id), espressoIsDisplayed()))
        }
        if (candidates.any { it.second.size > 1 }) return null
        val candidate = candidates.filter { it.second.size == 1 }.singleOrNull() ?: return null
        val view = candidate.second.single()
        return if (!ready || (view.isEnabled && isDisplayingAtLeast(CLICK_VISIBLE_PERCENTAGE).matches(view)))
            candidate.first else null
    }

    private fun uniquePresence(id: String, screenId: String?, condition: (View) -> Boolean): Boolean =
        onMainThread {
            val identity = AutomationIdMatcher.withAutomationId(id, screenId)
            val targets = resumedRoots().flatMap { descendants(it).toList() }
                .filter { it.isShown && identity.matches(it) }
            if (targets.size > 1) throw identityFailure(id, targets.size)
            targets.singleOrNull()?.let(condition) == true
        }

    private fun identityFailure(id: String, count: Int): AssertionError =
        AssertionError(if (count > 1) "TECHNICAL: duplicate AutomationId '${diagnosticId(id)}'."
            else "TECHNICAL: missing AutomationId '${diagnosticId(id)}'.")

    private fun waitForMatcher(description: String, matcher: Matcher<View>) {
        awaitCondition(description) { roots -> matchingViews(roots, matcher).size == 1 }
        try { onView(matcher).check(matches(matcher)) }
        catch (_: Throwable) { throw AssertionError("UI: visible text contract not satisfied uniquely.") }
    }

    private fun awaitLiveCondition(
        description: String,
        timeout: Long = LIVE_TIMEOUT_MILLISECONDS,
        failure: () -> AssertionError = { AssertionError(description) },
        condition: () -> Boolean
    ) {
        check(Looper.myLooper() != Looper.getMainLooper()) { "Live waits require the instrumentation thread." }
        val deadline = android.os.SystemClock.uptimeMillis() + timeout
        do {
            if (condition()) return
            android.os.SystemClock.sleep(100L)
        } while (android.os.SystemClock.uptimeMillis() < deadline)
        throw failure()
    }

    private fun awaitCondition(
        description: String,
        failure: () -> AssertionError = { AssertionError(description) },
        condition: (List<View>) -> Boolean
    ) {
        val resource = ViewConditionIdlingResource(description, TIMEOUT_SECONDS * 1_000L) {
            val roots = resumedRoots()
            roots.isNotEmpty() && condition(roots)
        }
        try {
            IdlingRegistry.getInstance().register(resource)
            resource.start()
            onIdle()
            if (resource.timedOut) throw failure()
            resource.failure?.let { throw AssertionError("TECHNICAL: UI observation failed.") }
        } catch (failure: AssertionError) {
            throw failure
        } catch (_: Throwable) {
            throw failure()
        } finally {
            IdlingRegistry.getInstance().unregister(resource)
            resource.close()
        }
    }
}

private fun resumedRoots(): List<View> =
    ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
        .filter { !it.isFinishing && !it.isDestroyed }
        .filter { it.hasWindowFocus() }
        .map { it.window.decorView }.distinct()

private fun descendants(view: View): Sequence<View> = sequence {
    yield(view)
    if (view is ViewGroup) {
        for (index in 0 until view.childCount) yieldAll(descendants(view.getChildAt(index)))
    }
}

private fun matchingViews(roots: List<View>, matcher: Matcher<View>): List<View> =
    roots.flatMap { descendants(it).filter(matcher::matches).toList() }.distinct()

private fun <T> onMainThread(action: () -> T): T {
    if (Looper.myLooper() == Looper.getMainLooper()) return action()
    var result: Result<T>? = null
    InstrumentationRegistry.getInstrumentation().runOnMainSync { result = runCatching(action) }
    return checkNotNull(result).getOrThrow()
}

private class ViewConditionIdlingResource(
    private val description: String,
    private val timeout: Long,
    private val condition: () -> Boolean
) : IdlingResource {
    private val handler = Handler(Looper.getMainLooper())
    private var closed = false
    private var deadline = 0L
    @Volatile private var idle = false
    @Volatile private var callback: IdlingResource.ResourceCallback? = null
    @Volatile var failure: Throwable? = null
        private set
    @Volatile var timedOut = false
        private set

    private val observation = object : Runnable {
        override fun run() {
            if (closed || idle) return
            val satisfied = try { condition() } catch (exception: Throwable) {
                failure = exception
                true
            }
            timedOut = !satisfied && android.os.SystemClock.uptimeMillis() >= deadline
            if (satisfied || timedOut) {
                idle = true
                callback?.onTransitionToIdle()
            } else handler.postDelayed(this, 100L)
        }
    }

    override fun getName(): String = description
    override fun isIdleNow(): Boolean = idle
    override fun registerIdleTransitionCallback(callback: IdlingResource.ResourceCallback) {
        this.callback = callback
    }
    fun start() = onMainThread {
        deadline = android.os.SystemClock.uptimeMillis() + timeout
        observation.run()
    }
    fun close() = onMainThread {
        closed = true
        handler.removeCallbacks(observation)
        callback = null
    }
}
