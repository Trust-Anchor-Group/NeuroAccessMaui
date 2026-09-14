package com.tag.neuroaccess.neuroaccessespressoautomationtests.identity

import android.os.Bundle
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ApplicationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityApprovalScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityRevocationScreen
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class PersonalIdReplacementFlowTest : BaseTest() {

    @Test
    fun approveRevokeAndApproveReplacement() {
        val arguments = InstrumentationRegistry.getArguments()
        assumeTrue("Requires manualApproval=true and revokeIdentity=true.",
            arguments.getString("manualApproval") == "true" &&
                arguments.getString("revokeIdentity") == "true")
        val configuredMinutes = arguments.getString("manualApprovalTimeoutMinutes")
        val minutes = if (configuredMinutes == null) 10 else
            configuredMinutes.toIntOrNull() ?: error("manualApprovalTimeoutMinutes must be an integer.")
        require(minutes in 1..60) { "manualApprovalTimeoutMinutes must be between 1 and 60." }
        val timeout = minutes * 60_000L

        PersonalIdApplicationFlow.submit()
        announce("First application submitted. Approve it in admin.")
        IdentityApprovalScreen.waitForApproval(timeout)
        val firstIdentityId = ApplicationsScreen.currentIdentityId()

        announce("First ID approved. Revoking it and verifying its Obsoleted state.")
        IdentityRevocationScreen.revokeAndReverify(firstIdentityId)

        PersonalIdApplicationFlow.submit(newApplication = true)
        announce("Replacement application submitted. Approve this second application in admin.")
        IdentityApprovalScreen.waitForApproval(timeout)
        val replacementIdentityId = ApplicationsScreen.currentIdentityId()
        check(replacementIdentityId != firstIdentityId) {
            "The approved replacement must have a different identity ID from the revoked identity."
        }
        announce("Completed: first ID revoked and a different replacement ID approved.")
    }

    private fun announce(message: String) {
        InstrumentationRegistry.getInstrumentation().sendStatus(2, Bundle().apply {
            putString("stream", "\n$message\n")
        })
    }
}
