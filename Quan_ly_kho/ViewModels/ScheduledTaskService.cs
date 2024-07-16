using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace Quan_ly_kho.ViewModels
{
    public class SchedulerState
    {
        public static System.Threading.Timer ScheduleCheckTimer { get; set; }
    }

    public class SchedulerTaskService : BaseViewModel
    {
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
            SchedulerState.ScheduleCheckTimer?.Dispose();
            Broker.Instance?.Disconnect();
        }

        private void OnBrokerMessageReceived(Document e)
        {
            var doc = e;

            if (doc["Type"]?.ToString() == "ack-schedule")
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

        private void UpdateDeviceStates(Room room, string state)
        {
            var listDevice = room.Device.ToList();
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var device in listDevice)
                {
                    if(device.DeviceState != null)
                    {
                        device.DeviceState.OrderByDescending(t => t.Timestamp).FirstOrDefault().State = state;
                    }
                    else
                    {
                        device.DeviceState.Add(new DeviceState { DeviceId = device.Id, State = state, Timestamp = DateTime.Now });
                    }
                }
                DataProvider.Ins.DB.SaveChanges();
                OnPropertyChanged();
            });

        }



        private void SendControlCommand(string buildingName, string token, int[] devices, string status)
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
                        var tasks = rooms.Select(async room =>
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

                                SendControlCommand(building.BuildingName, room.Id_esp32, devicesCount, "on");
                                _deviceCurrentState[room.Id_esp32] = "on";

                                // Đợi 3 giây để nhận phản hồi từ các thiết bị
                                await Task.Delay(3000);

                                List<string> unacknowledgedRooms;
                                List<string> acknowledgedRooms;
                                lock (_acknowledgedTokens)
                                {
                                    // lọc ra các phòng không gửi lại _acknowledgedTokens
                                    unacknowledgedRooms = rooms.Where(r => !_acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                    acknowledgedRooms = rooms.Where(r => _acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                }
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
                        }).ToList();

                        await Task.WhenAll(tasks);
                    }
                    else if (now > schedule.EndTime && now <= schedule.EndTime.AddMinutes(1.5))
                    {
                        // Gửi lệnh "off" cho tất cả các phòng chưa được tắt
                        var tasks = rooms.Select(async room =>
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

                                SendControlCommand(building.BuildingName, room.Id_esp32, devicesCount, "off");
                                _deviceCurrentState[room.Id_esp32] = "off";

                                // Đợi 3 giây để nhận phản hồi từ các thiết bị
                                await Task.Delay(3000);

                                List<string> unacknowledgedRooms;
                                List<string> acknowledgedRooms;
                                lock (_acknowledgedTokens)
                                {
                                    unacknowledgedRooms = rooms.Where(r => !_acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                    acknowledgedRooms = rooms.Where(r => _acknowledgedTokens.Contains(r.Id_esp32)).Select(r => r.Id_esp32).ToList();
                                }
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
                        }).ToList();

                        await Task.WhenAll(tasks);
                    }
                }
            }

        }

    }
}
