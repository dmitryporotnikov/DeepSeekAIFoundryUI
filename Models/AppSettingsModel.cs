namespace DeepSeekAIFoundryUI.Models
{
    /// <summary>
    /// Holds application-level settings for the AI model configuration.
    /// </summary>
    public class AppSettingsModel
    {
        public string ModelKey { get; set; }
        public string ModelEndpoint { get; set; }
        public string ModelName { get; set; }
    }
}
