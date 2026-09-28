package com.tag.neuroaccess.neuroaccessespressoautomationtests.flows.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.BiometricsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.NotificationPermissionPopup
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.PhoneCodeVerificationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.PhoneVerificationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.PinCreationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.UsernameScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.SuccessScreen

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.TestOtpClient

internal object RegistrationFlow {
    private companion object {
        const val UNITED_STATES_DIALING_CODE = "+1"
    }

    fun completeRegistration() {
        IdProviderScreen.assertDisplayed()
        IdProviderScreen.selectForMe()

        PhoneVerificationScreen.assertDisplayed()
        PhoneVerificationScreen.selectUnitedStates()
        PhoneVerificationScreen.enterPhoneNumber(TestData.phoneNumber())
        PhoneVerificationScreen.sendCode()

        PhoneCodeVerificationScreen.assertDisplayed()
        val phoneNumber = UNITED_STATES_DIALING_CODE + TestData.phoneNumber()
        val verificationCode = TestOtpClient.fetchVerificationCode(phoneNumber)
        PhoneCodeVerificationScreen.enterVerificationCode(verificationCode)
        PhoneCodeVerificationScreen.verifyCodeAndWaitFor(UsernameScreen.SCREEN)

        UsernameScreen.assertDisplayed()
        UsernameScreen.enterUsername(TestData.registrationUsername())
        UsernameScreen.continueToPinCreation()

        PinCreationScreen.assertDisplayed()
        PinCreationScreen.enterPin(TestData.pin())
        when (PinCreationScreen.createPinAndWaitForNextStep()) {
            BiometricsScreen.SCREEN -> BiometricsScreen.skip()
            SuccessScreen.SCREEN -> Unit
        }

        SuccessScreen.continueToHome()
        NotificationPermissionPopup.dismissAfterRegistration()
        HomeScreen.assertDisplayed()
    }
}
