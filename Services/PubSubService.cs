using Google.Api.Gax;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.PubSub.V1;
using Grpc.Core;
using Newtonsoft.Json;
using PubSubManager.DTOs;
using RestSharp;

namespace PubSubManager.Services
{
    public class PubSubService
    {
        private readonly ILogger logger;
        private readonly ConfigService configService;

        private readonly PublisherServiceApiClient publisherClient;
        private readonly SubscriberServiceApiClient subscriberClient;

        private Subscription Subscription;

        public PubSubService(ILogger<PubSubService> logger, ConfigService configService)
        {
            this.logger = logger;
            this.configService = configService;

            publisherClient = new PublisherServiceApiClientBuilder
            {
                EmulatorDetection = EmulatorDetection.EmulatorOrProduction
            }.Build();

            subscriberClient = new SubscriberServiceApiClientBuilder
            {
                EmulatorDetection = EmulatorDetection.EmulatorOrProduction
            }.Build();

        }

        public List<Topic> ListProjectTopics()
        {
            ProjectName projectName = ProjectName.FromProject(configService.ProjectName);

            IEnumerable<Topic> topics = publisherClient.ListTopics(projectName);

            return topics.ToList();

        }

        public Topic CreateTopic(string topicId)
        {
            var topicName = TopicName.FromProjectTopic(configService.ProjectName, topicId);
            Topic topic = null;

            try
            {
                topic = publisherClient.CreateTopic(topicName);
                Console.WriteLine($"Topic {topic.Name} created.");
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
            {
                Console.WriteLine($"Topic {topicName} already exists.");
            }
            return topic;
        }

        public List<Subscription> ListProjectSubscriptions()
        {
            ProjectName projectName = ProjectName.FromProject(configService.ProjectName);
            IEnumerable<Subscription> subs = this.subscriberClient.ListSubscriptions(projectName);

            return subs.ToList();
        }

        public Subscription CreateSubscription(string topicId, string subscriptionId)
        {
            TopicName topicName = TopicName.FromProjectTopic(configService.ProjectName, topicId);

            SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(configService.ProjectName, subscriptionId);
            Subscription subscription = null;

            try
            {
                subscription = this.subscriberClient.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 600);
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
            {
                // Already exists.  That's fine.
            }
            return subscription;
        }

        public async Task<int> PublishMessagesAsync(string topicId, IEnumerable<string> messages)
        {
            TopicName topicName = TopicName.FromProjectTopic(configService.ProjectName, topicId);
            return await PublishMessagesAsync(messages, topicName);
        }

        private async Task<int> PublishMessagesAsync(IEnumerable<string> messages, TopicName topicName)
        {
            PublisherClient publisher = await PublisherClient.CreateAsync(topicName, new PublisherClient.ClientCreationSettings()
                            .WithEmulatorDetection(EmulatorDetection.EmulatorOrProduction));

            int publishedMessageCount = 0;
            var publishTasks = messages.Select(async message =>
            {
                try
                {
                    string result = await publisher.PublishAsync(message);
                    Interlocked.Increment(ref publishedMessageCount);
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error ocurred when publishing message {message}: {exception.Message}");
                }
            });
            await Task.WhenAll(publishTasks);
            return publishedMessageCount;
        }

        public async Task<List<PubsubMessage>> PullMessagesAsync(string subscriptionId, bool acknowledge, int duration)
        {
            logger.LogInformation("Starting to pull messages");
            SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(configService.ProjectName, subscriptionId);

            SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName, new SubscriberClient.ClientCreationSettings().WithEmulatorDetection(EmulatorDetection.EmulatorOrProduction));

            logger.LogInformation($"Pulling from project: {configService.ProjectName} and subscriptionID: {subscriptionName.SubscriptionId}");

            int messageCount = 0;

            List<PubsubMessage> pulledMessages = new List<PubsubMessage>();

            Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
            {
                if (pulledMessages.Count(p => p.MessageId == message.MessageId) == 0)
                {
                    string text = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
                    logger.LogInformation($"Pulled message {message.MessageId}: {text}");
                    Interlocked.Increment(ref messageCount);
                    pulledMessages.Add(message);
                }
                return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
            });

            // Run for 5 seconds.
            await Task.Delay(duration * 1000);
            await subscriber.StopAsync(CancellationToken.None);

            // Lets make sure that the start task finished successfully after the call to stop.
            await startTask;
            return pulledMessages;
        }

        public async Task<List<PubsubMessage>> SendMessagesAsEvents(List<PubsubMessage> messages, Subscription subscription)
        {
            List<PubsubMessage> failedMessages = new List<PubsubMessage>();
            logger.LogInformation($"Sending events ...");
            foreach (var message in messages)
            {
                logger.LogInformation($"Sending message {message.MessageId}");
                var received = await SendEvent(subscription, System.Text.Encoding.UTF8.GetString(message.Data.ToArray()), message.MessageId, message.PublishTime.ToDateTime());

                if (!received)
                {
                    logger.LogInformation($"Sending message {message.MessageId} failed .. collecting ...");
                    failedMessages.Add(message);
                }
            }

            return failedMessages;
        }

        public async Task<int> SendMessages(List<PubsubMessage> messages, string topicid)
        {
            List<string> repeatingMessages = new List<string>();
            foreach (var message in messages)
            {
                repeatingMessages.Add(System.Text.Encoding.UTF8.GetString(message.Data.ToArray()));
            }

            logger.LogInformation($"Sending failed messages back to topic {topicid}");
            return await this.PublishMessagesAsync(topicid, repeatingMessages);
        }


        

        public async Task<bool> SendEvent(Subscription sub, string messageData, string messageId, DateTime? publishTime)
        {
            logger.LogInformation($"Sending {messageData} as an event.");
            var client = new RestClient($"{configService.EventDestinationUrl}");

            PubSubEvent pubSubEvent = new PubSubEvent()
            {
                Subscription = sub.Name,
                Message = new Message()
                {
                    PublishTime = (publishTime.HasValue ? publishTime.Value.ToUniversalTime() : DateTime.UtcNow),
                    Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageData)),
                    MessageId = messageId ?? Guid.NewGuid().ToString(),
                }
            };

            var request = new RestRequest()
                .AddBody(pubSubEvent)
                .AddHeader("Content-Type", "application/json; charset=utf-8")
                .AddHeader("ce-id", pubSubEvent.Message.MessageId)
                .AddHeader("ce-specversion", "1.0")
                .AddHeader("ce-type", "google.cloud.pubsub.topic.v1.messagePublished")
                //.AddHeader("ce-time", pubSubEvent.Message.PublishTime.ToUniversalTime())
                .AddHeader("ce-source", $"//pubsub.googleapis.com/{sub.Topic}");

            try
            {
                var response = await client.PostAsync(request);
                if(response.IsSuccessful)
                {
                    logger.LogInformation($"Message {messageData} successfuly sent.");
                    return true;
                }
                logger.LogWarning($"Destination didn't answer successfully. Destination url: {configService.EventDestinationUrl} Message data: {messageData} Response code: {response.StatusCode} Response error message: {response.ErrorMessage} Response content: {response.Content}");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error sending message trigger. Exception message: {ex.Message}");
            }

            return false;
        }

    }
}
