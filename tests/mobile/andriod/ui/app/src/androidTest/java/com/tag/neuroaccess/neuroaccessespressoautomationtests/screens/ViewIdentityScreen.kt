package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.view.KeyEvent
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.anyOf
import org.hamcrest.Matchers.equalTo
import org.hamcrest.Matchers.startsWith

object ViewIdentityScreen {
    const val SCREEN = "screen_view_identity"

    fun assertApprovedIdentity(identityId: String, firstName: String) {
        assertDisplayed()
        ScreenWaiter.waitForLiveText("displayed_identity_$identityId", equalTo("Approved"))
        ScreenWaiter.waitForLiveText("value_view_identity_name",
            anyOf(equalTo(firstName), startsWith("$firstName ")))
    }

    fun assertDisplayed() {
        ScreenWaiter.waitForAnyLive(SCREEN)
    }

    fun closeAndWaitFor(nextScreen: String) {
        assertDisplayed()
        InstrumentationRegistry.getInstrumentation().sendKeyDownUpSync(KeyEvent.KEYCODE_BACK)
        ScreenWaiter.waitFor(nextScreen)
    }
}
