package com.tag.neuroaccess.neuroaccessespressoautomationtests.identity

import android.os.Bundle
import androidx.test.espresso.Espresso.pressBack
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ApplicationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityApprovalScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ViewIdentityScreen
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class PersonalIdUpdateFlowTest : BaseTest() {

    @Test
    fun approveOscarThenAlbinAndVerifyShowId() {
        val arguments = InstrumentationRegistry.getArguments()
        assumeTrue("Requires manualApproval=true.", arguments.getString("manualApproval") == "true")
        val configuredMinutes = arguments.getString("manualApprovalTimeoutMinutes")
        val minutes = if (configuredMinutes == null) 10 else
            configuredMinutes.toIntOrNull() ?: error("manualApprovalTimeoutMinutes must be an integer.")
        require(minutes in 1..60) { "manualApprovalTimeoutMinutes must be between 1 and 60." }
        val timeout = minutes * 60_000L
        val oscar = TestData.personalIdentity().copy(firstName = "Oscar")
        val albin = oscar.copy(firstName = "Albin")

        PersonalIdApplicationFlow.submit(identity = oscar)
        announce("Approve the first application for Oscar in admin.")
        IdentityApprovalScreen.waitForApproval(timeout)
        val originalId = ApplicationsScreen.currentIdentityId()
        ApplicationsScreen.openApprovedIdentity()
        ViewIdentityScreen.assertApprovedIdentity(originalId, oscar.firstName)
        announce("Verified: Oscar is displayed with the first Approved identity ID.")
        ViewIdentityScreen.closeAndWaitFor(ApplicationsScreen.SCREEN)

        PersonalIdApplicationFlow.submitFromApplications(albin)
        announce("Approve the second application for Albin in admin.")
        IdentityApprovalScreen.waitForApproval(timeout, expectUnlockedDialog = false)
        val updatedId = ApplicationsScreen.currentIdentityId()
        check(updatedId != originalId) { "The approved second application must create a different identity ID." }
        pressBack()
        HomeScreen.assertDisplayed()
        HomeScreen.openPersonalId(TestData.pin())
        ViewIdentityScreen.assertApprovedIdentity(updatedId, albin.firstName)
        ViewIdentityScreen.closeAndWaitFor(HomeScreen.SCREEN)
        announce("Verified: Show ID displays Albin with the new Approved identity ID.")
    }

    private fun announce(message: String) {
        InstrumentationRegistry.getInstrumentation().sendStatus(2, Bundle().apply {
            putString("stream", "\n$message\n")
        })
    }
}
