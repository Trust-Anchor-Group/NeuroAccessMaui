package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.scrollTo
import androidx.test.espresso.matcher.ViewMatchers.isDisplayingAtLeast
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import org.hamcrest.Matchers.allOf

/** Handles the app's notification rationale during the first Home appearance after registration. */
object NotificationPermissionPopup {
    private const val POPUP = UiIds.PopupSystemPermission
    private const val SKIP = UiIds.ButtonSystemPermissionSkip
    private const val HOME_BANNER = UiIds.ImageMainHomeBanner

    /**
     * Waits for the rationale or completion of the first Home permission check, then dismisses it if needed.
     * HomeViewModel initially has ThemeLoaded=false and sets it only after its awaited permission flow.
     * This signal must not be reused for later Home appearances, where ThemeLoaded can already be true.
     * Native Android permission dialogs are separate from this app-owned rationale.
     */
    fun dismissAfterRegistration() {
        val instrumentation = androidx.test.platform.app.InstrumentationRegistry.getInstrumentation()
        val deadline = android.os.SystemClock.uptimeMillis() + 120_000L
        var nativePermissionHandled = false
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            val activePackage = root?.packageName?.toString()
            try {
                if (root != null && activePackage in setOf(
                        "com.android.permissioncontroller", "com.google.android.permissioncontroller")) {
                    check(!nativePermissionHandled) { "FLOW: an unexpected second system permission dialog appeared." }
                    this.allowSystemNotifications(root, checkNotNull(activePackage))
                    this.waitForSystemDialogDismissal(instrumentation, activePackage)
                    nativePermissionHandled = true
                }
            } finally {
                @Suppress("DEPRECATION")
                root?.recycle()
            }
            if (activePackage == instrumentation.targetContext.packageName && ScreenWaiter.isDisplayed(POPUP)) {
                UiContract.assertLabel(UiIds.LabelSystemPermissionTitle, UiText.text("Notifications"), POPUP)
                UiContract.assertLabel(UiIds.LabelSystemPermissionDescription, UiText.text("NotificationPopupDescription"), POPUP)
                onView(withAutomationId(SKIP, POPUP)).perform(scrollTo())
                ScreenWaiter.waitUntilReady(SKIP)
                UiContract.assertAction(SKIP, UiText.text("Skip"), enabled = true, screenId = POPUP)
                onView(allOf(withAutomationId(SKIP, POPUP), isDisplayingAtLeast(90), isEnabled()))
                    .perform(click())
                ScreenWaiter.waitUntilHidden(POPUP)
            }
            if (activePackage == instrumentation.targetContext.packageName &&
                ScreenWaiter.isDisplayed(HOME_BANNER) && !ScreenWaiter.isDisplayed(POPUP)) {
                UiContract.assertImage(HOME_BANNER, UiIds.ScreenMainHome)
                return
            }
            android.os.SystemClock.sleep(100L)
        } while (android.os.SystemClock.uptimeMillis() < deadline)
        error("FLOW: first Home notification/theme initialization did not complete within 120 seconds.")
    }

    private fun waitForSystemDialogDismissal(instrumentation: android.app.Instrumentation, packageName: String) {
        val deadline = android.os.SystemClock.uptimeMillis() + 10_000L
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            val dismissed = root != null && root.packageName?.toString() != packageName
            @Suppress("DEPRECATION")
            root?.recycle()
            if (dismissed) return
            android.os.SystemClock.sleep(100L)
        } while (android.os.SystemClock.uptimeMillis() < deadline)
        error("FLOW: native notification permission dialog did not close after acceptance.")
    }

    private fun allowSystemNotifications(root: android.view.accessibility.AccessibilityNodeInfo, packageName: String) {
        val systemLocale = androidx.test.platform.app.InstrumentationRegistry.getArguments()
            .getString("testSystemLanguageCode") ?: "en"
        check(systemLocale == "en") { "CONTRACT: this native notification dialog currently requires the English system locale." }
        val messages = root.findAccessibilityNodeInfosByViewId("$packageName:id/permission_message")
        val buttons = root.findAccessibilityNodeInfosByViewId("$packageName:id/permission_allow_button")
        try {
            check(messages.count { it.isVisibleToUser &&
                it.text?.toString()?.contains("notifications", ignoreCase = true) == true } == 1) {
                "UI: expected the native notification permission message."
            }
            val button = buttons.singleOrNull { it.isVisibleToUser }
                ?: error("TECHNICAL: expected one visible native permission_allow_button.")
            check(button.isEnabled && button.text?.toString() == "Allow" &&
                button.className?.toString() == "android.widget.Button") {
                "UI: native notification allow button differs from its English system contract."
            }
            check(button.performAction(android.view.accessibility.AccessibilityNodeInfo.ACTION_CLICK)) {
                "FLOW: native notification permission could not be accepted."
            }
        } finally {
            @Suppress("DEPRECATION")
            messages.forEach { it.recycle() }
            @Suppress("DEPRECATION")
            buttons.forEach { it.recycle() }
        }
    }
}
