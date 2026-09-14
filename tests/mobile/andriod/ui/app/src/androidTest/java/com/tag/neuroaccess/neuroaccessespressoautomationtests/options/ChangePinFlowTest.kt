package com.tag.neuroaccess.neuroaccessespressoautomationtests.options

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.BiometricsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PinAuthenticationPopup
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PinCreationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.SettingsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.SuccessScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ViewIdentityScreen
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class ChangePinFlowTest : BaseTest() {

    @Test
    fun changePinAndVerifyIdentityBeforeColdStart() {
        val currentPin = TestData.pin()
        val newPin = TestData.newPin()

        HomeScreen.assertDisplayed()
        HomeScreen.openSettings()
        SettingsScreen.assertDisplayed()
        SettingsScreen.openChangePin()

        PinAuthenticationPopup.assertDisplayed()
        PinAuthenticationPopup.enterPinAndWaitFor(currentPin, PinCreationScreen.SCREEN)

        PinCreationScreen.assertDisplayed()
        PinCreationScreen.enterPin(newPin)
        when (PinCreationScreen.createPinAndWaitForNextStep()) {
            BiometricsScreen.SCREEN -> BiometricsScreen.skip()
            SuccessScreen.SCREEN -> Unit
        }

        SuccessScreen.continueToHome()
        HomeScreen.openPersonalIdWithAuthenticatedSession()
        ViewIdentityScreen.assertDisplayed()
    }

    @Test
    fun verifyChangedPinAfterColdStart() {
        val oldPin = TestData.pin()
        val newPin = TestData.newPin()
        HomeScreen.assertDisplayed()
        HomeScreen.openPersonalIdAndWaitForPinPrompt()
        PinAuthenticationPopup.assertDisplayed()
        PinAuthenticationPopup.enterPinAndExpectRejection(oldPin)
        PinAuthenticationPopup.enterPinAndWaitFor(newPin, ViewIdentityScreen.SCREEN)
        ViewIdentityScreen.assertDisplayed()
    }
}
