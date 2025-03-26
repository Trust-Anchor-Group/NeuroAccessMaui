using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Pages.Applications.ApplyId
{
    public partial class ObservableAttachmentCard : ObservableObject
    {
        [ObservableProperty]
        private ImageSource? image;

        [ObservableProperty]
        private byte[]? imageBin;

        [ObservableProperty]
        private int imageRotation;

    }
}