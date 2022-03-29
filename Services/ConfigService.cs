namespace PubSubManager.Services
{
    public class ConfigService
    {
        public bool Emulator { get; set; }
        public string EmulatorUrl { get; set; }
        public string ProjectName { get; set; }
        public string EventDestinationUrl { get; set; }

        public ConfigService(IConfiguration configuration)
        {
            if(Environment.GetEnvironmentVariable("PUBSUB_EMULATOR_HOST") is not null)
            {
                Emulator = true;
                EmulatorUrl = Environment.GetEnvironmentVariable("PUBSUB_EMULATOR_HOST");
            }
            else
            {
                if(configuration["PUBSUB_EMULATOR_HOST"] is not null)
                {
                    Emulator = true;
                    EmulatorUrl = Environment.GetEnvironmentVariable("PUBSUB_EMULATOR_HOST");
                }
                
                Emulator = false;
                EmulatorUrl = "";
            }

            ProjectName = Environment.GetEnvironmentVariable("PUBSUB_PROJECT_ID") ?? (configuration["PUBSUB_PROJECT_ID"] ?? "kikker");

            EventDestinationUrl = Environment.GetEnvironmentVariable("EVENT_DESTINATION_URL") ?? (configuration["EVENT_DESTINATION_URL"] ?? "kikker");

        }
    }
}
