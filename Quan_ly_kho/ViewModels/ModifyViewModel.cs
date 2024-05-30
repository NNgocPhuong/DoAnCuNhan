using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Linq;
using System.Network;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Quan_ly_kho.ViewModels
{
    public class ModifyViewModel : BaseViewModel
    {
        public event EventHandler<Device> DeviceAdded;
        public event EventHandler<Device> DeviceEdited;
        public event EventHandler<Device> DeviceDeleted;
        private ObservableCollection<Device> _selectedDevices;
        private bool _errorMessageShown = false;
        public ObservableCollection<Device> SelectedDevices
        {
            get => _selectedDevices;
            set
            {
                _selectedDevices = value;
                OnPropertyChanged(nameof(SelectedDevices));
            }
        }

        private Device _selectedDevice;
        public Device SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));
                if (SelectedDevice != null)
                {
                    DeviceName = SelectedDevice.DeviceName;
                    DeviceType = SelectedDevice.DeviceType;
                    DeviceState = SelectedDevice.DeviceStateName;
                }
            }
        }
        private string _deviceName;
        public string DeviceName { get => _deviceName; set { _deviceName = value; OnPropertyChanged(); } }

        private string _deviceType;
        public string DeviceType { get => _deviceType; set { _deviceType = value; OnPropertyChanged(); } }

        private string _deviceState;
        public string DeviceState { get => _deviceState; set { _deviceState = value; OnPropertyChanged(); } }

        private Room _selectedRoom;
        public Room SelectedRoom { get => _selectedRoom; set { _selectedRoom = value; OnPropertyChanged(nameof(SelectedRoom)); } }
        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand OnCommand { get; set; }
        public ICommand OffCommand { get; set; }
        private Timer _keepAliveTimer;
        private DateTime _lastKeepAliveReceived;

        public ModifyViewModel(Room selected_Room)
        {
            SelectedDevices = new ObservableCollection<Device>();
            SelectedRoom = selected_Room;
            
            // Khởi tạo Timer để kiểm tra keep-alive mỗi 60 giây

            Task.Delay(60000).ContinueWith(_ =>
            {
                _keepAliveTimer = new Timer(CheckKeepAlive, null, 0, 60000);
            });

            // Lắng nghe trên topic của phòng
            Broker.Listen(SelectedRoom.Id_esp32, OnBrokerMessageReceived);
            #region ADD EDIT DELETE COMMAND
            AddCommand = new RelayCommand<object>(
                (p) =>
                {
                    if (string.IsNullOrEmpty(DeviceName))   
                    {
                        return false;
                    }
                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(x => x.DeviceName == DeviceName && x.RoomId == SelectedRoom.Id && x.DeviceType == DeviceType);
                    if (device != null)
                        return false;

                    return true;
                },
               async (p) =>
               {
                   var room = await DataProvider.Ins.DB.Room.FirstOrDefaultAsync(r => r.Id == SelectedRoom.Id);

                   if (room != null)
                   {
                       var newDevice = new Device()
                       {
                           DeviceName = DeviceName,
                           DeviceType = DeviceType,
                           RoomId = room.Id,
                           IsSelected = true,
                           DeviceState = new List<DeviceState> { new DeviceState { State = "Tắt" } },
                           //Schedule = new List<Schedule> { new Schedule { StartTime = DateTime.Now, EndTime = DateTime.Now.AddMinutes(1), Action = "Bật" } }
                       };
                       // Thêm thiết bị mới vào cơ sở dữ liệu
                       DataProvider.Ins.DB.Device.Add(newDevice);
                       try
                       {
                           await DataProvider.Ins.DB.SaveChangesAsync();
                       }
                       catch (DbEntityValidationException ex)
                       {
                           StringBuilder sb = new StringBuilder();
                           foreach (var eve in ex.EntityValidationErrors)
                           {
                               sb.AppendLine($"Entity of type \"{eve.Entry.Entity.GetType().Name}\" in state \"{eve.Entry.State}\" has the following validation errors:");
                               foreach (var ve in eve.ValidationErrors)
                               {
                                   sb.AppendLine($"- Property: \"{ve.PropertyName}\", Error: \"{ve.ErrorMessage}\"");
                               }
                           }
                           MessageBox.Show(sb.ToString(), "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                       }

                       // Cập nhật danh sách thiết bị được chọn
                       SelectedDevices.Add(newDevice);
                       DeviceAdded?.Invoke(this, newDevice);
                   }
               });

            EditCommand = new RelayCommand<object>(
                (p) =>
                {
                    if (string.IsNullOrEmpty(DeviceName))
                    {
                        return false;
                    }
                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(x => x.DeviceName == DeviceName
                    && x.RoomId == SelectedRoom.Id
                    && x.DeviceType == DeviceType);
                    if (device != null)
                        return false;
                    if (SelectedDevice == null)
                        return false;

                    return true;
                },
                (p) =>
                {
                    var room = DataProvider.Ins.DB.Room.FirstOrDefault(r => r.Id == SelectedRoom.Id);

                    if (room != null)
                    {
                        var device = DataProvider.Ins.DB.Device.Where(d => d.Id == SelectedDevice.Id).FirstOrDefault();
                        device.DeviceName = DeviceName;
                        device.DeviceType = DeviceType;
                        DataProvider.Ins.DB.SaveChanges();

                        // Cập nhật danh sách thiết bị được chọn
                        if (SelectedDevices.Contains(device))
                        {
                            SelectedDevices.Remove(device);
                            SelectedDevices.Add(device);
                        }
                        DeviceEdited?.Invoke(this, device);
                    }
                });
            DeleteCommand = new RelayCommand<object>(
                (p) =>
                {
                    if (SelectedDevice == null)
                        return false;
                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(x => x.Id == SelectedDevice.Id
                    && x.RoomId == SelectedRoom.Id);
                    if (device == null)
                        return false;


                    return true;
                },
                (p) =>
                {
                    var room = DataProvider.Ins.DB.Room
                    .Include(r => r.Device.Select(d => d.DeviceState))
                    .Include(r => r.Device.Select(d => d.Schedule))
                    .FirstOrDefault(r => r.Id == SelectedRoom.Id);

                    if (room != null)
                    {
                        var device = DataProvider.Ins.DB.Device
                            .Include(x => x.DeviceState)
                            .Include(x => x.Schedule)
                            .SingleOrDefault(d => d.Id == SelectedDevice.Id);

                        if (device != null)
                        {
                            // Xóa các thực thể liên quan trước
                            DataProvider.Ins.DB.DeviceState.RemoveRange(device.DeviceState);
                            DataProvider.Ins.DB.Schedule.RemoveRange(device.Schedule);

                            // Sau đó xóa thiết bị
                            DataProvider.Ins.DB.Device.Remove(device);

                            DataProvider.Ins.DB.SaveChanges();

                            // Cập nhật danh sách thiết bị được chọn
                            SelectedDevices.Remove(device);
                            DeviceDeleted?.Invoke(this, device);
                        }
                    }
                });
            #endregion

            OnCommand = new RelayCommand<object>(
                (p) => SelectedDevices.Count > 0,
                async (p) =>
                {
                    var controlMessage = new JObject
{
                    { "Type", "control" }
};

                    // Tạo đối tượng Devices
                    var devicesObject = new JObject();

                    // Tạo các mảng cho từng loại thiết bị
                    var doorsArray = new JArray();
                    var lightsArray = new JArray();
                    var fansArray = new JArray();
                    var airConditioningArray = new JArray();

                    // Thêm các thiết bị vào các mảng tương ứng
                    foreach (var device in SelectedDevices)
                    {
                        var deviceDetails = new JObject
                        {
                            { "id_esp", device.DeviceName },
                            { "power", "on" } 
                        };

                        if (device.DeviceType.StartsWith("Cửa") || device.DeviceType.StartsWith("cua"))
                        {
                            doorsArray.Add(deviceDetails);
                        }
                        else if (device.DeviceType.StartsWith("Đèn") || device.DeviceType.StartsWith("den"))
                        {
                            lightsArray.Add(deviceDetails);
                        }
                        else if (device.DeviceType.StartsWith("Quạt") || device.DeviceType.StartsWith("quat"))
                        {
                            fansArray.Add(deviceDetails);
                        }
                        else if (device.DeviceType.StartsWith("Điều hoà") || device.DeviceType.StartsWith("dieu hoa") || device.DeviceType.StartsWith("điều hoà"))
                        {
                            airConditioningArray.Add(deviceDetails);
                        }
                    }

                    // Thêm các mảng vào đối tượng Devices
                    devicesObject.Add("Doors", doorsArray);
                    devicesObject.Add("Lights", lightsArray);
                    devicesObject.Add("Fans", fansArray);
                    devicesObject.Add("AirConditionings", airConditioningArray); 

                    // Thêm đối tượng Devices vào thông điệp điều khiển
                    controlMessage.Add("Devices", devicesObject);

                    // Gửi thông điệp
                    Broker.Send(SelectedRoom.Id_esp32, controlMessage.ToString());

                    // Chờ phản hồi và cập nhật trạng thái
                    await ListenForResponseAndUpdateState("on");
                });
            OffCommand = new RelayCommand<object>(
                (p) => SelectedDevices.Count > 0,
                async (p) =>
                {
                    var controlMessage = new JObject
{
                    { "Type", "control" }
};

                    // Tạo đối tượng Devices
                    var devicesObject = new JObject();

                    // Tạo các mảng cho từng loại thiết bị
                    var doorsArray = new JArray();
                    var lightsArray = new JArray();
                    var fansArray = new JArray();
                    var airConditioningArray = new JArray();

                    // Thêm các thiết bị vào các mảng tương ứng
                    foreach (var device in SelectedDevices)
                    {
                        var deviceDetails = new JObject
                        {
                            { "id_esp", device.DeviceName },
                            { "power", "off" } 
                        };

                        if (device.DeviceType.StartsWith("Cửa") || device.DeviceType.StartsWith("cua"))
                        {
                            doorsArray.Add(deviceDetails);
                        }
                        else if (device.DeviceType.StartsWith("Đèn") || device.DeviceType.StartsWith("den"))
                        {
                            lightsArray.Add(deviceDetails);
                        }
                        else if (device.DeviceType.StartsWith("Quạt") || device.DeviceType.StartsWith("quat"))
                        {
                            fansArray.Add(deviceDetails);
                        }
                        else if (device.DeviceType.StartsWith("Điều hoà") || device.DeviceType.StartsWith("dieu hoa") || device.DeviceType.StartsWith("điều hoà"))
                        {
                            airConditioningArray.Add(deviceDetails);
                        }
                    }

                    // Thêm các mảng vào đối tượng Devices
                    devicesObject.Add("Doors", doorsArray);
                    devicesObject.Add("Lights", lightsArray);
                    devicesObject.Add("Fans", fansArray);
                    devicesObject.Add("AirConditionings", airConditioningArray); 

                    // Thêm đối tượng Devices vào thông điệp điều khiển
                    controlMessage.Add("Devices", devicesObject);

                    // Gửi thông điệp
                    Broker.Send(SelectedRoom.Id_esp32, controlMessage.ToString());

                    // Chờ phản hồi và cập nhật trạng thái
                    await ListenForResponseAndUpdateState("off");
                });
        }

        private void OnBrokerMessageReceived(Document doc)
        {
            // Kiểm tra nếu là bản tin keep-alive
            if (doc["Type"]?.ToString() == "keep-alive")
            {
                _lastKeepAliveReceived = DateTime.Now;
            }
        }
        private void CheckKeepAlive(object state)
        {
            if ((DateTime.Now - _lastKeepAliveReceived).TotalSeconds > 65)
            {
                // Nếu quá 65 giây mà không nhận được bản tin keep-alive, cập nhật trạng thái thiết bị
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var device in SelectedDevices)
                    {
                        var deviceState = device.DeviceState.FirstOrDefault();
                        if (deviceState != null)
                        {
                            deviceState.State = "Lỗi";
                        }
                        else
                        {
                            device.DeviceState.Add(new DeviceState { DeviceId = device.Id, State = "Lỗi" });
                            DataProvider.Ins.DB.SaveChanges();
                        }
                    }

                    OnPropertyChanged(nameof(SelectedDevices));

                    // Hiển thị thông báo lỗi
                    if (!_errorMessageShown)
                    {
                        MessageBox.Show("Vi xử lý bị lỗi", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        _errorMessageShown = true;
                        foreach (var device in SelectedDevices)
                        {
                            DeviceState itemState = new DeviceState
                            {
                                DeviceId = device.Id,
                                State = "Lỗi"
                            };
                            device.DeviceState.Add(itemState);
                            DataProvider.Ins.DB.SaveChanges();
                            OnPropertyChanged();
                        }
                        // Dừng và xoá timer
                        _keepAliveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                        _keepAliveTimer?.Dispose();
                    }

                });
            }
        }
        private async Task ListenForResponseAndUpdateState(string command)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var tcs = new TaskCompletionSource<bool>();

            Action<Document> responseHandler = null;
            responseHandler = (doc) =>
            {
                if (doc["Type"]?.ToString() == "ack-control" && doc["Response"]?.ToString() == "control-success")
                {
                    tcs.TrySetResult(true);
                }
            };

            Broker.process_received_data += responseHandler;

            try
            {
                // Ensure listening on the correct topic
                Broker.Listen(SelectedRoom.Id_esp32, responseHandler);

                var result = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));

                if (result == tcs.Task && tcs.Task.Result)
                {
                    foreach (var device in SelectedDevices)
                    {
                        var deviceState = device.DeviceState.FirstOrDefault();
                        if (deviceState != null)
                        {
                            deviceState.State = command == "on" ? "Bật" : "Tắt";
                        }
                        else
                        {
                            device.DeviceState.Add(new DeviceState
                            {
                                DeviceId = device.Id,
                                State = command == "on" ? "Bật" : "Tắt"
                            });
                            DataProvider.Ins.DB.SaveChanges();
                        }
                        OnPropertyChanged();
                        
                    }
                }
                else
                {
                    foreach (var device in SelectedDevices)
                    {
                        var deviceState = device.DeviceState.FirstOrDefault();
                        if (deviceState != null)
                        {
                            deviceState.State = "Lỗi";
                        }
                        else
                        {
                            device.DeviceState.Add(new DeviceState
                            {
                                DeviceId = device.Id,
                                State = "Lỗi"
                            });
                            DataProvider.Ins.DB.SaveChanges();
                        }
                        OnPropertyChanged();
                        
                    }
                }
            }
            catch (OperationCanceledException)
            {
                foreach (var device in SelectedDevices)
                {
                    var deviceState = device.DeviceState.FirstOrDefault();
                    if (deviceState != null)
                    {
                        deviceState.State = "Lỗi";
                    }
                    else
                    {
                        device.DeviceState.Add(new DeviceState
                        {
                            DeviceId = device.Id,
                            State = "Lỗi"
                        });
                        DataProvider.Ins.DB.SaveChanges();
                    }
                    OnPropertyChanged();
                }
            }
            finally
            {
                OnPropertyChanged(nameof(SelectedDevices));
                Broker.process_received_data -= responseHandler;

                _lastKeepAliveReceived = DateTime.Now;
            }
        }
    }
}

    

