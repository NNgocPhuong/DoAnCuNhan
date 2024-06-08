using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Quan_ly_kho.ViewModels
{
    public class SchedulerState
    {
        public static System.Threading.Timer KeepAliveTimer { get; set; }
        public static System.Threading.Timer ScheduleCheckTimer { get; set; }
    }

    public class SchedulerTaskService : BaseViewModel
    {
        public List<Building> Buildings { get => DataProvider.Ins.DB.Building.ToList(); }
        public List<Room> Rooms { get => DataProvider.Ins.DB.Room.ToList(); }
        public List<Schedule> Schedules { get => DataProvider.Ins.DB.Schedule.ToList(); }

        private Dictionary<string, DateTime> _deviceLastKeepAlive = new Dictionary<string, DateTime>();
        private HashSet<string> _acknowledgedTokens = new HashSet<string>();

        public SchedulerTaskService()
        {

            
        }
        public void Start()
        {
            if (SchedulerState.KeepAliveTimer == null)
            {
                SchedulerState.KeepAliveTimer = new System.Threading.Timer(CheckKeepAlive, null, 0, 60000);
            }

            if (SchedulerState.ScheduleCheckTimer == null)
            {
                SchedulerState.ScheduleCheckTimer = new System.Threading.Timer(CheckAndExecuteSchedules, null, 0, 60000);
            }

            // Lắng nghe topic của các tòa nhà
            foreach (var building in Buildings)
            {
                Broker.Listen(building.BuildingName.ToMD5(), OnBrokerMessageReceived);
            }
        }
        public void Stop()
        {
            SchedulerState.KeepAliveTimer?.Dispose();
            SchedulerState.ScheduleCheckTimer?.Dispose();
            Broker?.Disconnect();
        }
        private void OnBrokerMessageReceived(Document e)
        {
            var doc = e;

            if (doc["Type"]?.ToString() == "keep-alive")
            {
                var token = doc["Token"]?.ToString();
                if (token != null)
                {
                    lock (_deviceLastKeepAlive)
                    {
                        _deviceLastKeepAlive[token] = DateTime.Now;
                    }
                }
            }
            else if (doc["Type"]?.ToString() == "ack-schedule")
            {
                var token = doc["Token"]?.ToString();
                if (token != null)
                {
                    lock (_acknowledgedTokens)
                    {
                        _acknowledgedTokens.Add(token);
                    }
                }
            }
        }

        public void CheckKeepAlive(object state)
        {
            var now = DateTime.Now;
            List<KeyValuePair<string, DateTime>> deviceList;

            lock (_deviceLastKeepAlive)
            {
                deviceList = _deviceLastKeepAlive.ToList();
            }

            foreach (var kvp in deviceList)
            {
                if ((now - kvp.Value).TotalMinutes > 2.5)
                {
                    var room = Rooms.FirstOrDefault(r => r.Id_esp32 == kvp.Key);
                    if (room != null)
                    {
                        UpdateDeviceStates(room, "Lỗi");
                    }

                    lock (_deviceLastKeepAlive)
                    {
                        _deviceLastKeepAlive.Remove(kvp.Key);
                    }
                }
            }
        }

        private void UpdateDeviceStates(Room room, string state)
        {
            foreach (var device in room.Device)
            {
                var deviceState = device.DeviceState.LastOrDefault();
                if (deviceState != null)
                {
                    deviceState.State = state;
                }
                else
                {
                    device.DeviceState.Add(new DeviceState { DeviceId = device.Id, State = state });
                }
            }
            DataProvider.Ins.DB.SaveChanges();
        }

        private async Task SendControlCommand(string buildingName, string token, int[] devices, string status)
        {
            var doc = new Document
            {
                Type = "schedule",
                Token = token,
                Devices = devices,
                Status = status
            };

            Broker.Send(buildingName.ToMD5(), doc);
        }

        public async void CheckAndExecuteSchedules(object state)
        {
            var now = DateTime.Now;
            foreach (var building in Buildings)
            {
                var rooms = Rooms.Where(r => r.Floor.BuildingId == building.Id).ToList();
                foreach (var schedule in Schedules.Where(s => now >= s.StartTime && now <= s.EndTime))
                {
                    var devicesCount = new int[3]; // 0: Lights, 1: Fans, 2: Doors

                    foreach (var room in rooms)
                    {
                        foreach (var device in room.Device)
                        {
                            switch (device.DeviceType)
                            {
                                case "Đèn":
                                    devicesCount[0]++;
                                    break;
                                case "Quạt":
                                    devicesCount[1]++;
                                    break;
                                case "Cửa":
                                    devicesCount[2]++;
                                    break;
                            }
                        }

                        lock (_acknowledgedTokens)
                        {
                            _acknowledgedTokens.Clear();
                        }

                        // Gửi lệnh điều khiển tới tất cả các phòng trong tòa nhà
                        await SendControlCommand(building.BuildingName, room.Id_esp32, devicesCount, "on");

                        // Đợi 8 giây để nhận phản hồi
                        await Task.Delay(8000);

                        List<string> unacknowledgedRooms;
                        // Kiểm tra token đã nhận phản hồi
                        lock (_acknowledgedTokens)
                        {
                            unacknowledgedRooms = rooms.Where(r => !_acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                        }

                        foreach (var unackRoom in unacknowledgedRooms)
                        {
                            var Room = rooms.FirstOrDefault(r => r.Id_esp32 == unackRoom);
                            if (Room != null)
                            {
                                UpdateDeviceStates(Room, "Lỗi");
                            }
                        }
                    }
                }
            }
        }
    }
}

