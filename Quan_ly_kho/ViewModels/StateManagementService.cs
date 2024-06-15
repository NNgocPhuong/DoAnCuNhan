using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Quan_ly_kho.ViewModels
{
    public class StateManagementService
    {
        private readonly object _lock = new object();

        private class DeviceMonitoringInfo
        {
            public Timer Timer { get; set; }
            public DateTime LastKeepAliveTime { get; set; }
            public bool ErrorMessageShown { get; set; }
        }

        private readonly Dictionary<string, DeviceMonitoringInfo> _deviceMonitoringInfos = new Dictionary<string, DeviceMonitoringInfo>();

        public void StartMonitoring(string deviceId)
        {
            lock (_lock)
            {
                if (!_deviceMonitoringInfos.ContainsKey(deviceId))
                {
                    var timer = new Timer(CheckKeepAlive, deviceId, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                    _deviceMonitoringInfos[deviceId] = new DeviceMonitoringInfo
                    {
                        Timer = timer,
                        LastKeepAliveTime = DateTime.Now,
                        ErrorMessageShown = false
                    };
                }
            }
            App.KeepAliveService.StartListening(deviceId);
        }

        public void StopMonitoring(string deviceId)
        {
            lock (_lock)
            {
                if (_deviceMonitoringInfos.TryGetValue(deviceId, out var deviceInfo))
                {
                    deviceInfo.Timer.Dispose();
                    _deviceMonitoringInfos.Remove(deviceId);
                }
            }
            //App.KeepAliveService.StopListening(deviceId);
        }

        public void UpdateKeepAlive(string deviceId)
        {
            lock (_lock)
            {
                if (_deviceMonitoringInfos.TryGetValue(deviceId, out var deviceInfo))
                {
                    deviceInfo.LastKeepAliveTime = DateTime.Now;
                    deviceInfo.ErrorMessageShown = false;
                }
            }
        }

        private void CheckKeepAlive(object state)
        {
            string deviceId = (string)state;
            lock (_lock)
            {
                if (_deviceMonitoringInfos.TryGetValue(deviceId, out var deviceInfo))
                {
                    if ((DateTime.Now - deviceInfo.LastKeepAliveTime).TotalMinutes > 2)
                    {
                        if (!deviceInfo.ErrorMessageShown)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Vi xử lý {deviceId} không phản hồi", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            deviceInfo.ErrorMessageShown = true;
                        }
                    }
                }
            }
        }
    }

}
