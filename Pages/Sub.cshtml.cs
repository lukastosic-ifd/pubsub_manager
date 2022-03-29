using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PubSubManager.Services;

namespace PubSubManager.Pages
{
    public class SubModel : PageModel
    {
        private readonly PubSubService pubSubService;
        private readonly ILogger logger;

        public SubModel(PubSubService pubSubService, ILogger<SubModel> logger)
        {
            this.pubSubService = pubSubService;
            this.logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string SubId { get; set; }

        public Subscription Subscription { get; set; }

        public int SuccessSingleEvent { get; set; } = 0;

        public int NumberOfMessages { get; set; } = -2;

        public List<PubsubMessage> Messages { get; set; } = new List<PubsubMessage>();

        private void FindSub(string topic)
        {
            if (string.IsNullOrWhiteSpace(SubId))
            {
                return;
            }

            this.Subscription = pubSubService.ListProjectSubscriptions().Where(s => s.SubscriptionName.SubscriptionId == SubId).FirstOrDefault();
           
        }

        public void OnGet()
        {
            FindSub(SubId);

            if(Subscription is null)
            {
                return ;
            }            
        }

        public async Task OnPostFetchmessages(bool autoack, string subId, string time)
        {
            var timeInt = int.Parse(time);

            Messages = await pubSubService.PullMessagesAsync(subId, autoack, timeInt);

            OnGet();
        }

        public async Task OnPostSingleEvent(string eventmessage, string subId)
        {
            FindSub(SubId);

            if(await pubSubService.SendEvent(Subscription, eventmessage, null, null))
            {
                SuccessSingleEvent = 1;
            }
            else
            {
                SuccessSingleEvent = 2;
            }

            OnGet();
        }

        public async Task OnPostMessagestoevents(string time, string subId)
        {
            FindSub(SubId);

            var timeInt = int.Parse(time);

            Messages = await pubSubService.PullMessagesAsync(subId, true, timeInt);

            logger.LogInformation($"Found {Messages.Count} to send as events");

            if(Messages.Count == 0)
            {
                return;
            }

            var failedMessages = await pubSubService.SendMessagesAsEvents(Messages, Subscription);
            logger.LogInformation($"Failed messages: {failedMessages.Count}");

            if(failedMessages.Count == 0)
            {
                NumberOfMessages = -1;
                return;
            }

            NumberOfMessages = await pubSubService.SendMessages(failedMessages, Subscription.TopicAsTopicName.TopicId);

            OnGet();
        }

        //public async Task OnPostConvertMessages(List<PubsubMessage> messages, Subscription subscription)
        //{

        //}
    }
}
