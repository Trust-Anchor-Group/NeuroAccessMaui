package com.tag.neuroaccess.neuroaccessespressoautomationtests.quicklogin

import android.widget.EditText
import android.util.Log
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityDetailsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PinAuthenticationPopup
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ViewIdentityScreen
import org.hamcrest.Matchers.allOf
import org.junit.Test
import org.junit.runner.RunWith

/**
 * Signs into the real Quick Login page through manual QR entry and verifies its identity table.
 * Requires an English app on Home with an approved identity, connectivity, camera permission,
 * PIN authentication (biometrics disabled), and the configured testPin argument.
 */
@RunWith(AndroidJUnit4::class)
class QuickLoginFlowTest : BaseTest() {
    /** Reads the mobile identity, signs in through Scan QR and compares the web identity ID. */
    @Test
    fun openQuickLoginThroughManualQrEntry() {
        Log.i("QuickLoginTest", "START: Quick Login identity comparison")
        HomeScreen.assertDisplayed()
        val pin = TestData.pin()
        HomeScreen.openPersonalId(pin)
        val identityId = IdentityDetailsScreen.openAndReadNeuroId()
        Log.i("QuickLoginTest", "MOBILE: Show ID / Neuro-ID = $identityId")
        ViewIdentityScreen.closeAndWaitFor(HomeScreen.SCREEN)

        QuickLoginWebSession().use { page ->
            onView(allOf(withText("Scan QR"), isDisplayed())).perform(click())
            ScreenWaiter.waitForText("Enter QR manually")
            onView(allOf(withText("Enter QR manually"), isDisplayed())).perform(click())
            ScreenWaiter.waitForText("Open")

            page.open()
            val signUrl = page.awaitSignUrl()
            val purpose = page.purpose()
            onView(allOf(isAssignableFrom(EditText::class.java), isDisplayed()))
                .perform(replaceText(signUrl), closeSoftKeyboard())
            onView(allOf(withText("Open"), isDisplayed())).perform(click())

            ScreenWaiter.waitForText(purpose)
            onView(allOf(withText("Accept"), isDisplayed())).perform(click())
            PinAuthenticationPopup.assertDisplayed()
            PinAuthenticationPopup.enterPinAndWaitFor(pin, HomeScreen.SCREEN)
            page.awaitApprovedIdentity(identityId)
        }
    }
}


