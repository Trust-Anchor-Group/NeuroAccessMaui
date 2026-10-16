package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.app.Activity
import android.content.ComponentName
import android.content.Intent
import androidx.test.core.app.ActivityScenario

object NeuroAccessApp {

    private const val TARGET_APPLICATION_PACKAGE = "com.tag.NeuroAccess"
    private const val TARGET_ACTIVITY_NAME = "com.tag.NeuroAccess.MainActivity"

    fun launch(): ActivityScenario<Activity> {
        val launchIntent = Intent(Intent.ACTION_MAIN)
            .addCategory(Intent.CATEGORY_LAUNCHER)
            .setComponent(
                ComponentName(TARGET_APPLICATION_PACKAGE, TARGET_ACTIVITY_NAME)
            )

        return ActivityScenario.launch(launchIntent)
    }
}
