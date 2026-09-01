package com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding

import android.util.Log
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PhoneCodeVerificationScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PhoneVerificationScreen
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class IdProviderTest : BaseTest() {

    private companion object {
        const val TEST_LOG_TAG = "IdProviderTest"
    }

    @Test
    fun selectForMeNavigatesToPhoneCodeVerificationAndBack() {
        this.logStep("1/10: Verifying ID Provider screen")
        IdProviderScreen.assertDisplayed()
        this.logStep("PASS: ID Provider screen is displayed")

        this.logStep("2/10: Selecting ID provider automatically")
        IdProviderScreen.selectForMe()

        this.logStep("3/10: Verifying Phone Verification screen")
        PhoneVerificationScreen.assertDisplayed()
        this.logStep("PASS: Phone Verification screen is displayed")

        this.logStep("4/10: Entering test phone number")
        PhoneVerificationScreen.enterPhoneNumber(TestData.PHONE_NUMBER)

        this.logStep("5/10: Sending phone code")
        PhoneVerificationScreen.sendCode()

        this.logStep("6/10: Verifying Phone Code Verification screen")
        PhoneCodeVerificationScreen.assertDisplayed()
        this.logStep("PASS: Phone Code Verification screen is displayed")

        this.logStep("7/10: Returning to Phone Verification")
        PhoneCodeVerificationScreen.goBack()

        this.logStep("8/10: Verifying Phone Verification screen after Back")
        PhoneVerificationScreen.assertDisplayed()
        this.logStep("PASS: Phone Verification screen is displayed after Back")

        this.logStep("9/10: Returning to ID Provider")
        PhoneVerificationScreen.goBack()

        this.logStep("10/10: Verifying ID Provider screen after Back")
        IdProviderScreen.assertDisplayed()
        this.logStep("PASS: ID Provider screen is displayed after Back")
    }

    private fun logStep(message: String) {
        Log.i(TEST_LOG_TAG, message)
    }
}
