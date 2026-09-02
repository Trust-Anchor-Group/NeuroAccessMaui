package com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.TestOtpClient
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.BiometricsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.NotificationPermissionPopup
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PhoneCodeVerificationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PhoneVerificationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PinCreationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.UsernameScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.SuccessScreen
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class RegistrationFlowTest : BaseTest() {
    private companion object {
        const val UNITED_STATES_DIALING_CODE = "+1"
    }

    @Test
    fun testUserCompletesRegistrationAndReachesHomePage() {
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
        NotificationPermissionPopup.dismissIfDisplayed()
        HomeScreen.assertDisplayed()
    }
}


