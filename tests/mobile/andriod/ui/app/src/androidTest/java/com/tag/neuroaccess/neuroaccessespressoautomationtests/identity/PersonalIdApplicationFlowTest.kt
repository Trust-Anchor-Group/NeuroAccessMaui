package com.tag.neuroaccess.neuroaccessespressoautomationtests.identity

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class PersonalIdApplicationFlowTest : BaseTest() {

    @Test
    fun testUserAppliesForPersonalId() {
        PersonalIdApplicationFlow.submit()
    }
}
