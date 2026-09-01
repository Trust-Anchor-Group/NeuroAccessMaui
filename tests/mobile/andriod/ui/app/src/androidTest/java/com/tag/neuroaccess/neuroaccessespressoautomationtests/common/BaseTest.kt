package com.tag.neuroaccess.neuroaccessespressoautomationtests.common

import android.app.Activity
import androidx.test.core.app.ActivityScenario
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.NeuroAccessApp
import org.junit.After
import org.junit.Before

abstract class BaseTest {

    private var applicationScenario: ActivityScenario<Activity>? = null

    @Before
    fun launchApplication() {
        this.applicationScenario = NeuroAccessApp.launch()
    }

    @After
    fun closeApplication() {
        this.applicationScenario?.close()
    }
}
