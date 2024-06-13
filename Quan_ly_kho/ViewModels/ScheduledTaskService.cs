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
        //public static System.Threading.Timer KeepAliveTimer { get; set; }
        public static System.Threading.Timer ScheduleCheckTimer { get; set; }
    }

    public class SchedulerTaskService : BaseViewModel
    {
        //private Dictionary<string, DateTime> _deviceLastKeepAlive = new Dictionary<string, DateTime>();
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
            //if (SchedulerState.KeepAliveTimer == null)
            //{
            //    SchedulerState.KeepAliveTimer = new System.Threading.Timer(CheckKeepAlive, null, 0, 60000);
            //}

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
            //SchedulerState.KeepAliveTimer?.Dispose();
            SchedulerState.ScheduleCheckTimer?.Dispose();
            Broker.Instance?.Disconnect();
        }

        private void OnBrokerMessageReceived(Document e)
        {
            var doc = e;

            //if (doc["Type"]?.ToString() == "keep-alive")
            //{
            //    var token = doc["Token"]?.ToString();
            //    if (token != null)
            //    {
            //        lock (_deviceLastKeepAlive)
            //        {
            //            _deviceLastKeepAlive[token] = DateTime.Now;
            //        }
            //    }
            //}
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

        //public void CheckKeepAlive(object state)
        //{
        //    var now = DateTime.Now;
        //    List<KeyValuePair<string, DateTime>> deviceList;

        //    lock (_deviceLastKeepAlive)
        //    {
        //        deviceList = _deviceLastKeepAlive.ToList();
        //    }
        //    foreach (var kvp in deviceList)
        //    {
        //        if ((now - kvp.Value).TotalMinutes > 2.5)
        //        {
        //            var room = Rooms.FirstOrDefault(r => r.Id_esp32 == kvp.Key);
        //            if (room != null)
        //            {
        //                UpdateDeviceStates(room, "Lỗi");
        //            }

        //            lock (_deviceLastKeepAlive)
        //            {
        //                _deviceLastKeepAlive.Remove(kvp.Key);
        //            }
        //        }
        //    }
        //}

        private void UpdateDeviceStates(Room room, string state)
        {
            var deviceStatesToAdd = new List<DeviceState>();
            var devicesToUpdate = new List<DeviceState>();

            foreach (var device in room.Device)
            {
                // Đảm bảo DeviceState được khởi tạo nếu nó đang null
                if (device.DeviceState == null)
                {
                    device.DeviceState = new HashSet<DeviceState>();
                }

                // Tìm trạng thái hiện tại của thiết bị
                var existingDeviceState = device.DeviceState
                    .OrderByDescending(ds => ds.Timestamp) // Đảm bảo lấy trạng thái mới nhất nếu có nhiều trạng thái
                    .FirstOrDefault();

                if (existingDeviceState != null)
                {
                    // Nếu trạng thái đã tồn tại, cập nhật trạng thái
                    existingDeviceState.State = state;
                    existingDeviceState.Timestamp = DateTime.Now; // Cập nhật thời gian nếu cần
                    devicesToUpdate.Add(existingDeviceState);
                }
                else
                {
                    // Nếu trạng thái chưa tồn tại, thêm mới
                    var deviceState = new DeviceState
                    {
                        DeviceId = device.Id,
                        State = state,
                        Timestamp = DateTime.Now // Cập nhật thời gian nếu cần
                    };

                    deviceStatesToAdd.Add(deviceState);
                }
            }

            // Áp dụng các thay đổi sau khi vòng lặp hoàn tất
            foreach (var deviceState in deviceStatesToAdd)
            {
                var device = room.Device.FirstOrDefault(d => d.Id == deviceState.DeviceId);
                if (device != null)
                {
                    // Đảm bảo DeviceState được khởi tạo nếu nó đang null
                    if (device.DeviceState == null)
                    {
                        device.DeviceState = new HashSet<DeviceState>();
                    }
                    device.DeviceState.Add(deviceState);
                }
            }

            // Lưu các thay đổi trong một bước duy nhất
            using (var transaction = DataProvider.Ins.DB.Database.BeginTransaction())
            {
                try
                {
                    DataProvider.Ins.DB.DeviceState.AddRange(deviceStatesToAdd);
                    foreach (var deviceState in devicesToUpdate)
                    {
                        DataProvider.Ins.DB.Entry(deviceState).State = EntityState.Modified;
                    }
                    DataProvider.Ins.DB.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Xử lý hoặc ghi log lỗi nếu cần thiết
                    throw new InvalidOperationException("Đã xảy ra lỗi khi cập nhật trạng thái thiết bị.", ex);
                }
            }
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
