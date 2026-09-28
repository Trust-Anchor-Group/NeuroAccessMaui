package com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.flows.onboarding.RegistrationFlow
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class RegistrationFlowTest : BaseTest() {
    @Test
    fun testUserCompletesRegistrationAndReachesHomePage() {
        RegistrationFlow.completeRegistration()
    }
}
