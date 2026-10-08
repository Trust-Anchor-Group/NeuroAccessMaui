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
    private const val UNITED_STATES_DIALING_CODE = "+1"

    fun completeRegistration() {
        check(androidx.test.platform.app.InstrumentationRegistry.getArguments()
            .getString("testRegistrationMode") == "temporary-phone") {
            "Registration currently requires explicit testRegistrationMode=temporary-phone; ordinary email onboarding is not migrated yet."
        }
        val phoneNumber = TestData.phoneNumber()
        val pin = TestData.registrationPin()
        val username = TestData.registrationUsername()
        TestOtpClient.validateConfiguration()

        IdProviderScreen.assertDisplayed()
        IdProviderScreen.selectRequestedLanguage()
        android.util.Log.i("NeuroTestFlow", "Welcome and requested locale verified")
        IdProviderScreen.selectForMe()

        PhoneVerificationScreen.assertDisplayed()
        PhoneVerificationScreen.selectUnitedStates()
        android.util.Log.i("NeuroTestFlow", "USA country selected")
        PhoneVerificationScreen.enterPhoneNumber(phoneNumber)
        PhoneVerificationScreen.sendCode()

        PhoneCodeVerificationScreen.assertDisplayed()
        val verificationCode = TestOtpClient.fetchVerificationCode(UNITED_STATES_DIALING_CODE + phoneNumber)
        PhoneCodeVerificationScreen.enterVerificationCode(verificationCode)
        PhoneCodeVerificationScreen.verifyTemporaryPhoneCode()
        android.util.Log.i("NeuroTestFlow", "Phone verified; real temporary-OTP path skipped email")

        UsernameScreen.assertDisplayed()
        UsernameScreen.enterUsername(username)
        UsernameScreen.continueToPinCreation()
        android.util.Log.i("NeuroTestFlow", "Account created; current credential screen reached")

        PinCreationScreen.assertDisplayed()
        PinCreationScreen.enterPin(pin)
        when (PinCreationScreen.createPinAndWaitForNextStep(expectTemporaryOtpWarning = true)) {
            BiometricsScreen.SCREEN -> BiometricsScreen.skip()
            SuccessScreen.SCREEN -> Unit
        }
        android.util.Log.i("NeuroTestFlow", "Local credential and temporary-OTP warning verified")

        SuccessScreen.continueToHome(waitForHome = false)
        NotificationPermissionPopup.dismissAfterRegistration()
        HomeScreen.assertDisplayed()
        HomeScreen.assertFreshAccount()
        android.util.Log.i("NeuroTestFlow", "Registration complete; real Home actions verified")
    }
}
