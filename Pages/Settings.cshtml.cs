using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PubSubManager.Services;

namespace PubSubManager.Pages
{
    public class SettingsModel : PageModel
    {

        public ConfigService ConfigService;

        public SettingsModel(ConfigService configService)
        {
            this.ConfigService = configService;
        }
        
        public void OnGet()
        {

        }

        public void OnPostSavesettings(string newproject, bool newemulator, string newemulatorurl, string newdestinationurl)
        {
            ConfigService.ProjectName = newproject;
            ConfigService.Emulator = newemulator;
            ConfigService.EmulatorUrl = newemulatorurl;
            ConfigService.EventDestinationUrl = newdestinationurl;
            OnGet();
        }
    }
}
