using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;

namespace Quan_ly_kho.ViewModels
{
    public class SchedulerState
    {
        public static System.Threading.Timer KeepAliveTimer { get; set; }
        public static System.Threading.Timer ScheduleCheckTimer { get; set; }
    }

    public class SchedulerTaskService : BaseViewModel
    {
        private Dictionary<string, DateTime> _deviceLastKeepAlive = new Dictionary<string, DateTime>();
        private HashSet<string> _acknowledgedTokens = new HashSet<string>();
        private Dictionary<string, string> _deviceCurrentState = new Dictionary<string, string>();
        public List<Building> Buildings { get; set; }
        public List<Room> Rooms { get; set; }
        public SchedulerTaskService()
        {
            Buildings = DataProvider.Ins.DB.Building.ToList();
            Rooms = DataProvider.Ins.DB.Room.Include(r => r.Floor).ToList();
        }

        public void Start()
        {
            if (SchedulerState.KeepAliveTimer == null)
            {
                SchedulerState.KeepAliveTimer = new System.Threading.Timer(CheckKeepAlive, null, 0, 60000);
            }

            if (SchedulerState.ScheduleCheckTimer == null)
            {
                SchedulerState.ScheduleCheckTimer = new System.Threading.Timer(CheckAndExecuteSchedules, null, 0, 30000);
            }

            // Lắng nghe topic của các tòa nhà
            foreach (var building in Buildings)
            {
                Broker.Instance.Listen(building.BuildingName.ToMD5(), OnBrokerMessageReceived);
            }
        }

        public void Stop()
        {
            SchedulerState.KeepAliveTimer?.Dispose();
            SchedulerState.ScheduleCheckTimer?.Dispose();
            Broker.Instance?.Disconnect();
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
                device.DeviceState.Add(new DeviceState { DeviceId = device.Id, State = state });
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

            Broker.Instance.Send(buildingName.ToLower().ToMD5(), doc);
        }

        public async void CheckAndExecuteSchedules(object state)
        {
            var now = DateTime.Now;
           
            foreach (var building in Buildings)
            {
                List<Room> rooms = new List<Room>();
                List<Schedule> schedules = new List<Schedule>();

                rooms = Rooms.Where(r => r.Floor.BuildingId == building.Id).ToList();
                schedules = DataProvider.Ins.DB.Schedule.Where(s => s.Device.Room.Floor.BuildingId == building.Id).ToList();

                foreach (var schedule in schedules)
                {
                    if (now >= schedule.StartTime && now <= schedule.EndTime)
                    {
                        // Gửi lệnh "on" cho tất cả các phòng chưa được bật
                        foreach (var room in rooms)
                        {
                            if (!_deviceCurrentState.ContainsKey(room.Id_esp32) || _deviceCurrentState[room.Id_esp32] != "on")
                            {
                                var devicesCount = new int[3]; // 0: Lights, 1: Fans, 2: Doors
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

                                await SendControlCommand(building.BuildingName, room.Id_esp32, devicesCount, "on");
                                //Đánh dấu rằng lệnh on đã được gửi tới phòng hiện tại
                                _deviceCurrentState[room.Id_esp32] = "on";

                                // Đợi 8 giây để nhận phản hồi
                                await Task.Delay(8000);

                                List<string> unacknowledgedRooms;
                                List<string> acknowledgedRooms;
                                lock (_acknowledgedTokens)
                                {
                                    //Lọc ra các phòng trong danh sách rooms mà ID (r.Id_esp32) không có trong _acknowledgedTokens.
                                    unacknowledgedRooms = rooms.Where(r => !_acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                    // Danh sách các phòng đã nhận được phản hồi
                                    acknowledgedRooms = rooms.Where(r => _acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                }
                                // Cập nhật trạng thái thiết bị trong các phòng đã nhận được phản hồi thành "Bật"
                                foreach (var ackRoom in acknowledgedRooms)
                                {
                                    var Room = rooms.FirstOrDefault(r => r.Id_esp32 == ackRoom);
                                    if (Room != null)
                                    {
                                        UpdateDeviceStates(Room, "Bật");
                                    }
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
                    else if (now > schedule.EndTime && now <= schedule.EndTime.AddMinutes(1.5))
                    {
                        // Gửi lệnh "off" cho tất cả các phòng chưa được tắt
                        foreach (var room in rooms)
                        {
                            if (_deviceCurrentState.ContainsKey(room.Id_esp32) && _deviceCurrentState[room.Id_esp32] != "off")
                            {
                                var devicesCount = new int[3]; // 0: Lights, 1: Fans, 2: Doors
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

                                await SendControlCommand(building.BuildingName, room.Id_esp32, devicesCount, "off");
                                _deviceCurrentState[room.Id_esp32] = "off";

                                // Đợi 8 giây để nhận phản hồi
                                await Task.Delay(8000);

                                List<string> unacknowledgedRooms;
                                List<string> acknowledgedRooms;
                                lock (_acknowledgedTokens)
                                {
                                    unacknowledgedRooms = rooms.Where(r => !_acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                    acknowledgedRooms = rooms.Where(r => _acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                }
                                // Cập nhật trạng thái thiết bị trong các phòng đã nhận được phản hồi thành "Tắt"
                                foreach (var ackRoom in acknowledgedRooms)
                                {
                                    var Room = rooms.FirstOrDefault(r => r.Id_esp32 == ackRoom);
                                    if (Room != null)
                                    {
                                        UpdateDeviceStates(Room, "Tắt");
                                    }
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

    }
}
