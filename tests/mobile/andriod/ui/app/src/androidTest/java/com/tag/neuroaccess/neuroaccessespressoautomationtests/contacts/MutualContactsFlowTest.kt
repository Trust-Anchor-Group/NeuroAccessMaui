package com.tag.neuroaccess.neuroaccessespressoautomationtests.contacts

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.flows.contacts.MutualContactsFlow
import org.junit.Test
import org.junit.runner.RunWith

/** Device phases coordinated by the Linux two-device runner; requires two distinct accounts. */
@RunWith(AndroidJUnit4::class)
class MutualContactsFlowTest : BaseTest() {
    /** Runs only the phase selected by the two-device runner, preserving account storage. */
    @Test
    fun runDevicePhase() {
        MutualContactsFlow.runDevicePhase()
    }
}
