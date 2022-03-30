using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PubSubManager.Services;

namespace PubSubManager.Pages
{
    public class TopicModel : PageModel
    {
        private readonly PubSubService pubSubService;

        public TopicModel(PubSubService pubSubService)
        {
            this.pubSubService = pubSubService;
        }

        [BindProperty(SupportsGet = true)]
        public string TopicId { get; set; }

        public Topic Topic { get; set; }
        public List<Subscription> Subscriptions { get; set; }

        private void FindTopic(string topic)
        {
            TopicId = topic;
            if (string.IsNullOrWhiteSpace(TopicId))
            {
                return;
            }

            this.Topic = pubSubService.ListProjectTopics().Where(t => t.TopicName.TopicId == TopicId).FirstOrDefault();

            if (Topic is null)
            {
                return;
            }

            this.Subscriptions = pubSubService.ListProjectSubscriptions().Where(s => s.Topic == Topic.Name).ToList();
        }

        public void OnGet()
        {
            FindTopic(TopicId);

            if(Topic is null)
            {
                return ;
            }            
        }

        public async Task OnPostNewMessageAsync(string topicid, string newmessage)
        {
            if (newmessage is not null)
            {
                await pubSubService.PublishMessagesAsync(topicid, new List<string> { newmessage });
            }
            this.TopicId = topicid;
            OnGet();
        }

        public void OnPostNewSub(string newsub, string parenttopic)
        {
            this.pubSubService.CreateSubscription(parenttopic, newsub);
            FindTopic(parenttopic);            
        }

        public void OnPostNewPushSub(string newpushsub, string parenttopic)
        {
            FindTopic(parenttopic);
            Subscriptions.Add(this.pubSubService.CreatePushSubscription(parenttopic, newpushsub));
        }

        public void OnPostDeleteSub(string subid, string topicid)
        {
            this.pubSubService.DeleteSubscription(subid);
            FindTopic(topicid);
        }
    }
}
