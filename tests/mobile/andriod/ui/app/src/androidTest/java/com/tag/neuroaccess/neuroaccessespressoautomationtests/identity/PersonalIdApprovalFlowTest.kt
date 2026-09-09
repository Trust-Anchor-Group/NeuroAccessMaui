package com.tag.neuroaccess.neuroaccessespressoautomationtests.identity

import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityApprovalScreen
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class PersonalIdApprovalFlowTest : BaseTest() {

    @Test
    fun submitAndWaitForManualApproval() {
        val arguments = InstrumentationRegistry.getArguments()
        assumeTrue(
            "Manual approval requires the manualApproval=true instrumentation argument.",
            arguments.getString("manualApproval") == "true"
        )
        val configuredMinutes = arguments.getString("manualApprovalTimeoutMinutes")
        val timeoutMinutes = if (configuredMinutes == null) 10 else
            configuredMinutes.toIntOrNull()
                ?: error("manualApprovalTimeoutMinutes must be an integer from 1 to 60.")
        require(timeoutMinutes in 1..60) {
            "manualApprovalTimeoutMinutes must be between 1 and 60."
        }

        PersonalIdApplicationFlow.submit()
        IdentityApprovalScreen.waitForApproval(timeoutMinutes * 60_000L)
    }
}
