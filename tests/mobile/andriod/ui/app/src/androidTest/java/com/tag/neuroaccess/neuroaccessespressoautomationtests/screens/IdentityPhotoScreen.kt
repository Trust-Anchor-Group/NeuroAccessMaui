package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.app.Activity
import android.app.Instrumentation.ActivityResult
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.Color
import androidx.core.content.FileProvider
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.intent.Intents
import androidx.test.espresso.intent.Intents.intending
import androidx.test.espresso.intent.matcher.IntentMatchers.hasType
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import java.io.File

object IdentityPhotoScreen {
    private const val UPLOAD_PHOTO = "button_kyc_upload_photo_profilephoto"
    private const val CROP_SCREEN = "screen_image_cropping"
    private const val ACCEPT_CROP = "button_accept_image_crop"
    private const val TEST_IMAGE_FILE_NAME = "identity-test-photo.jpg"

    fun uploadGeneratedTestImage() {
        val imageUri = createTestImage()
        val resultData = Intent()
            .setData(imageUri)
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)

        Intents.init()
        try {
            intending(hasType("image/*"))
                .respondWith(
                    ActivityResult(
                        Activity.RESULT_OK,
                        resultData
                    )
                )

            onView(withAutomationId(UPLOAD_PHOTO))
                .perform(click())

            ScreenWaiter.waitFor(CROP_SCREEN)
            onView(withAutomationId(ACCEPT_CROP))
                .perform(click())
            KycProcessScreen.waitForSelfiePage()
        } finally {
            Intents.release()
        }
    }

    private fun createTestImage() =
        InstrumentationRegistry.getInstrumentation().targetContext.let { context ->
            val imageFile = File(context.cacheDir, TEST_IMAGE_FILE_NAME)
            val bitmap = Bitmap.createBitmap(720, 960, Bitmap.Config.ARGB_8888)
            bitmap.eraseColor(Color.rgb(67, 97, 138))
            imageFile.outputStream().use { outputStream ->
                check(bitmap.compress(Bitmap.CompressFormat.JPEG, 95, outputStream)) {
                    "The generated identity test image could not be encoded."
                }
            }
            bitmap.recycle()

            FileProvider.getUriForFile(
                context,
                "${context.packageName}.fileProvider",
                imageFile
            )
        }
}
