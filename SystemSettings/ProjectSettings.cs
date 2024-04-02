using FileStorage;
using Newtonsoft.Json;

namespace Project.SystemSettings
{
    public class ProjectSettings
    {
        public static readonly string DefaultName = "Agol";
        public static readonly string DefaultTheme = "ultima-indigo-compact";
        public static readonly string DefaultLogoIconUrl = "favicon.ico";
        public static readonly string DefaultLogoImageUrl = "/assets/images/logo.png";

        private string _theme;


        public string Name { get; set; } = DefaultName;

        public string ThemeCode
        {
            get => string.IsNullOrEmpty(_theme) ? DefaultTheme : _theme;
            set => _theme = value;
        }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public FileDetailsDTO LogoImage { get; set; }

        public int? LogoImageId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public FileDetailsDTO LogoIcon { get; set; }

        public int? LogoIconId { get; set; }
    }
}
