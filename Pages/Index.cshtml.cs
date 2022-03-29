using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PubSubManager.Services;

namespace PubSubManager.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public readonly PubSubService pubSubService;

        public List<Topic> Topics { get; set; }

        public List<Subscription> Subscriptions { get; set; }

        public IndexModel(ILogger<IndexModel> logger, PubSubService pubSubService)
        {
            _logger = logger;

            this.pubSubService = pubSubService;

            Topics = this.pubSubService.ListProjectTopics();
            Subscriptions = this.pubSubService.ListProjectSubscriptions();
            
        }

        public void OnGet()
        {

        }

        public void OnPostNewTopic(string newtopic)
        {
            Topics.Add(this.pubSubService.CreateTopic(newtopic));
        }

        public void OnPostNewSub(string newsub, string parenttopic)
        {
            Subscriptions.Add(this.pubSubService.CreateSubscription(parenttopic, newsub));
        }
    }
}