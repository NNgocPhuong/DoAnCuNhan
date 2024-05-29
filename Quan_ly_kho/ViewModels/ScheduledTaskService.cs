//using Newtonsoft.Json.Linq;
//using Quan_ly_kho.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Timers;

//namespace Quan_ly_kho.ViewModels
//{
//    public class ScheduledTaskService
//    {
//        private Timer _timer;
//        private List<Schedule> _schedules;
//        private Room _selectedRoom;

//        public ScheduledTaskService(Room selectedRoom)
//        {
//            _schedules = new List<Schedule>();
//            _selectedRoom = selectedRoom;

//            // Khởi tạo và cấu hình timer
//            _timer = new Timer(60000); // Kiểm tra mỗi phút
//            _timer.Elapsed += OnTimerElapsed;
//            _timer.Start();
//        }

//        public void AddSchedule(Schedule schedule)
//        {
//            _schedules.Add(schedule);
//        }

//        public void RemoveSchedule(Schedule schedule)
//        {
//            _schedules.Remove(schedule);
//        }

//        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
//        {
//            var currentTime = DateTime.Now;

//            foreach (var schedule in _schedules.Where(s => s.StartTime <= currentTime && s.EndTime >= currentTime).ToList())
//            {
//                ExecuteSchedule(schedule.Device, schedule.Action);
//            }
//        }

//        private void ExecuteSchedule(Device device, string action)
//        {
//            var controlMessage = new JObject
//        {
//            { "Code", "Control" }
//        };

//            var deviceDetails = new JObject
//        {
//            { "id_esp", device.DeviceName },
//            { "power", action }
//        };

//            if (device.DeviceType.StartsWith("Cửa"))
//            {
//                controlMessage.Add("Doors", new JArray { deviceDetails });
//            }
//            else if (device.DeviceType.StartsWith("Đèn"))
//            {
//                controlMessage.Add("Lights", new JArray { deviceDetails });
//            }
//            else if (device.DeviceType.StartsWith("Quạt"))
//            {
//                controlMessage.Add("Fans", new JArray { deviceDetails });
//            }
//            else if (device.DeviceType.StartsWith("Điều hoà"))
//            {
//                controlMessage.Add("Air Conditionings", new JArray { deviceDetails });
//            }
//            BaseViewModel.Broker.Connect();
//            BaseViewModel.Broker.Send(_selectedRoom.Id_esp32, controlMessage.ToString());

//            if (action == "on")
//            {
//                var New_state = new DeviceState()
//                {
//                    DeviceId = device.Id,
//                    State = "Bật"
//                };
//                device.DeviceState.Add(New_state);
//            }
//            else if (action == "off")
//            {
//                var New_state = new DeviceState()
//                {
//                    DeviceId = device.Id,
//                    State = "Tắt"
//                };
//                device.DeviceState.Add(New_state);
//            }
//        }
//    }
//}

