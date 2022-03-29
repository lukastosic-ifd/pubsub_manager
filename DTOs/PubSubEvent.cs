using Newtonsoft.Json;

namespace PubSubManager.DTOs
{
    public class PubSubEvent
    {
        public string Subscription { get; set; }
        public Message Message { get; set; }


        //        {
        //  "subscription": "projects/my-project/subscriptions/my-subscription",
        //  "message": {
        //    "@type": "type.googleapis.com/google.pubsub.v1.PubsubMessage",
        //    "attributes": {
        //      "attr1":"attr1-value"
        //    },
        //    "data": "dGVzdCBtZXNzYWdlIDM=",
        //    "messageId": "message-id",
        //    "publishTime":"2021-02-05T04:06:14.109Z"
        //  }
        //}
    }

    public class Message
    {
        private DateTime publishTime;
        
        [JsonProperty("@type")]
        public string Type { get; set; } = "type.googleapis.com/google.pubsub.v1.PubsubMessage";
        public Dictionary<string,string> Attributes { get; set; }
        public string Data { get; set; }
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public DateTime PublishTime 
        { 
            get
            {
                return publishTime.ToUniversalTime();
            }
            set
            {
                publishTime = value;
            } 
        } 
    }
}
