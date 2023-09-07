using NeuroAccessMaui.Resources.Languages;
using Microsoft.Extensions.Localization;

namespace NeuroAccessMaui
{
    [ContentProperty(nameof(Key))]
    public class LocalizeExtension : IMarkupExtension
    {
        private readonly IStringLocalizer<AppResources> localizer;

        public string Key { get; set; } = string.Empty;

        public LocalizeExtension()
        {
			this.localizer = ServiceHelper.GetService<IStringLocalizer<AppResources>>();
        }

        public object ProvideValue(IServiceProvider ServiceProvider)
        {
            string LocalizedText = this.localizer[this.Key];
            return LocalizedText;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider ServiceProvider) => this.ProvideValue(ServiceProvider);
    }
}
