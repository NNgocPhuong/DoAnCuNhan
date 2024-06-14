using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quan_ly_kho.ViewModels
{
    public class KeepAliveService
    {
        private readonly object _lock = new object();
        private readonly HashSet<string> _listeningDevices = new HashSet<string>();

        public void StartListening(string deviceId)
        {
            lock (_lock)
            {
                if (!_listeningDevices.Contains(deviceId))
                {
                    Broker.Instance.process_received_data += OnBrokerMessageReceived;
                    Broker.Instance.Listen(deviceId, OnBrokerMessageReceived);
                    _listeningDevices.Add(deviceId);
                }
            }
        }

        private void OnBrokerMessageReceived(Document doc)
        {
            if (doc["Type"]?.ToString() == "keep-alive")
            {
                string deviceId = doc["_id"]?.ToString();
                if (!string.IsNullOrEmpty(deviceId))
                {
                    App.StateManagementService.UpdateKeepAlive(deviceId);
                }
            }
        }
        // Hàm này thật ra không dùng
        public void StopListening(string deviceId)
        {
            lock (_lock)
            {
                if (_listeningDevices.Contains(deviceId))
                {
                    _listeningDevices.Remove(deviceId);
                    // Only unsubscribe if no devices are being listened to
                    if (_listeningDevices.Count == 0)
                    {
                        Broker.Instance.process_received_data -= OnBrokerMessageReceived;
                    }
                    Broker.Instance.Unsubscribe(deviceId);
                }
            }
        }
    }
}
