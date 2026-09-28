package com.tag.neuroaccess.neuroaccessespressoautomationtests.options

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.ViewIdentityScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.BiometricsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.PinCreationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.options.SettingsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.PinAuthenticationPopup
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.SuccessScreen

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
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
