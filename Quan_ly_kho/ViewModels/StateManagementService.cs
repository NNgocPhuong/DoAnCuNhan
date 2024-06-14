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
        private readonly Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();
        private readonly Dictionary<string, DateTime> _lastKeepAliveTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, bool> _errorMessageShown = new Dictionary<string, bool>();
        private readonly object _lock = new object();

        public void StartMonitoring(string deviceId)
        {
            lock (_lock)
            {
                if (!_timers.ContainsKey(deviceId))
                {
                    var timer = new Timer(CheckKeepAlive, deviceId, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                    _timers[deviceId] = timer;
                    _lastKeepAliveTimes[deviceId] = DateTime.Now;
                    _errorMessageShown[deviceId] = false;
                }
            }
            App.KeepAliveService.StartListening(deviceId);
        }

        public void StopMonitoring(string deviceId)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(deviceId, out var timer))
                {
                    timer.Dispose();
                    _timers.Remove(deviceId);
                    _lastKeepAliveTimes.Remove(deviceId);
                    _errorMessageShown.Remove(deviceId);
                }
            }
            //App.KeepAliveService.StopListening(deviceId);
        }

        public void UpdateKeepAlive(string deviceId)
        {
            lock (_lock)
            {
                if (_lastKeepAliveTimes.ContainsKey(deviceId))
                {
                    _lastKeepAliveTimes[deviceId] = DateTime.Now;
                    _errorMessageShown[deviceId] = false;
                }
            }
        }

        private void CheckKeepAlive(object state)
        {
            string deviceId = (string)state;
            lock (_lock)
            {
                if (_lastKeepAliveTimes.TryGetValue(deviceId, out var lastKeepAliveTime))
                {
                    if ((DateTime.Now - lastKeepAliveTime).TotalMinutes > 2)
                    {
                        if (!_errorMessageShown[deviceId])
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Vi xử lý ở phòng {deviceId} không phản hồi", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            _errorMessageShown[deviceId] = true;
                        }
                    }
                }
            }
        }
    }

}
