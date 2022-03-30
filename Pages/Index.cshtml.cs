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
        }

        public void OnGet()
        {
            Topics = this.pubSubService.ListProjectTopics();
            Subscriptions = this.pubSubService.ListProjectSubscriptions();
        }

        public void OnPostNewTopic(string newtopic)
        {
            this.pubSubService.CreateTopic(newtopic);
            OnGet();
        }

        public void OnPostNewSub(string newsub, string parenttopic, bool pushtype)
        {
            this.pubSubService.CreateSubscription(parenttopic, newsub);
            OnGet();
        }

        public void OnPostNewPushSub(string newsub, string parenttopic)
        {
            this.pubSubService.CreatePushSubscription(parenttopic, newsub);
            OnGet();
        }

        public void OnPostDeleteTopic(string topicid)
        {
            this.pubSubService.DeleteTopic(topicid);
            OnGet();
        }

        public void OnPostDeleteSub(string subid)
        {
            this.pubSubService.DeleteSubscription(subid);
            OnGet();
        }

    }
}