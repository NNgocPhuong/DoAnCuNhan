using MQTT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public class Broker : Client
    {
        public Broker() : base("broker.emqx.io") { }
        public void Send(string topic, Document doc)
        {
            Publish(topic, doc?.ToString() ?? "{}");
        }
        protected override void RaiseDataRecieved(string topic, byte[] message)
        {

            var content = message.UTF8();
            var doc = Document.Parse(content);

            process_received_data?.Invoke(doc);

            base.RaiseDataRecieved(topic, message);
        }

        public Action<Document> process_received_data;
        string last_topic;

        public void Listen(string topic, Action<Document> received_callback)
        {
            process_received_data = received_callback;
            if (last_topic != null)
            {
                Unsubscribe(last_topic);
            }
            last_topic = topic;
            if (topic != null) Subscribe(topic);
        }
    }
}
