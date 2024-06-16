using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Quan_ly_kho.Models;
using System.Collections.ObjectModel;
using System.Data.Entity;
using MaterialDesignThemes.Wpf;
using Quan_ly_kho.Views;
using Newtonsoft.Json.Linq;

namespace Quan_ly_kho.ViewModels
{
    public class ManageViewModel : BaseViewModel
    {
        #region Khai báo biến
        public ICommand ModifyWindowCommand { get; set; }
        private ObservableCollection<Device> _devices;
        public ICommand ScheduleRoomCommand { get; set; }
        //public ICommand ScheduleBuildingCommand { get; set; }
        public ObservableCollection<Device> Devices
        {
            get => _devices;
            set
            {
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }
        private bool _isAllSelected;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged(nameof(IsAllSelected));
                    SelectAllDevices(_isAllSelected);
                }
            }
        }

        private Room _selectedRoom;
        public Room SelectedRoom {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged(nameof(SelectedRoom));
            }
        }
        private Building _selectedBuilding;
        public Building SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                _selectedBuilding = value;
                OnPropertyChanged();
            }
        }
        private void SelectAllDevices(bool isSelected)
        {
            foreach (var item in Devices)
            {
                item.IsSelected = isSelected;
            }
        }
        #endregion

        public ManageViewModel(Room selected_Room, Building selected_Building)
        {
            Devices = new ObservableCollection<Device>();
            SelectedRoom = selected_Room;
            SelectedBuilding = selected_Building;

            //string s = (SelectedBuilding.BuildingName.ToLower() + SelectedRoom.RoomNumber).ToMD5();
            //SelectedRoom.Id_esp32 = s;
            //DataProvider.Ins.DB.SaveChanges();
            Broker.Instance.Listen(SelectedRoom.Id_esp32, received_callback);
            Document doc = new Document()
            {
                Type = "request-infor"
            };
            Broker.Instance.Send(SelectedRoom.Id_esp32, doc);

            ModifyWindowCommand = new RelayCommand<object>((p) => { return true; },
                (p) =>
                {
                    var selectedDevices = Devices.Where(d => d.IsSelected).ToList();
                    var modifyViewModel = new ModifyViewModel(SelectedRoom)
                    {
                        SelectedDevices = new ObservableCollection<Device>(selectedDevices)
                    };
                    modifyViewModel.DeviceAdded -= ModifyViewModel_DeviceAdded; // Đảm bảo hủy đăng ký trước khi đăng ký mới
                    modifyViewModel.DeviceAdded += ModifyViewModel_DeviceAdded; // Đăng ký sự kiện
                    modifyViewModel.DeviceEdited -= ModifyViewModel_DeviceEdited; // Đảm bảo hủy đăng ký trước khi đăng ký mới
                    modifyViewModel.DeviceEdited += ModifyViewModel_DeviceEdited;
                    modifyViewModel.DeviceDeleted -= ModifyViewModel_DeviceDeleted; // Đảm bảo hủy đăng ký trước khi đăng ký mới
                    modifyViewModel.DeviceDeleted += ModifyViewModel_DeviceDeleted;

                    ModifyWindow w = new ModifyWindow(modifyViewModel);
                    w.Closed += (sender, args) => modifyViewModel.Dispose();
                    w.ShowDialog();
                });
            ScheduleRoomCommand = new RelayCommand<object>((p) => { return true; }, 
                (p) => 
                {
                    var selectedDevices = Devices.Where(d => d.IsSelected).ToList();
                    
                    var scheduleViewModel = new ScheduleViewModel(SelectedRoom)
                    {
                        SelectedDevices = new ObservableCollection<Device>(selectedDevices)
                    };
                    ScheduleWindow w = new ScheduleWindow(scheduleViewModel);
                    w.ShowDialog();
                });
            //ScheduleBuildingCommand = new RelayCommand<object>((p) => { return true; },
            //    (p) =>
            //    {
            //        var buildingScheduleViewModel = new BuildingScheduleViewModel(SelectedBuilding);
            //        BuildingScheduleWindow w = new BuildingScheduleWindow(buildingScheduleViewModel);
            //        w.ShowDialog();
            //    });
        }
        private void UpdateRoomIdEsp32(int roomId, string idEsp32)
        {
            var room = DataProvider.Ins.DB.Room.Find(roomId);
            if (room != null)
            {
                room.Id_esp32 = idEsp32;
                DataProvider.Ins.DB.SaveChanges();
            }
        }
        private void ModifyViewModel_DeviceAdded(object sender, Device e)
        {
            if (!Devices.Contains(e))
            {
                Devices.Add(e);
            }
        }
        private void ModifyViewModel_DeviceEdited(object sender, Device e)
        {
            if (Devices.Contains(e))
            {
                Devices.Remove(e);
                Devices.Add(e);
            }
        }
        private void ModifyViewModel_DeviceDeleted(object sender, Device e)
        {
            if (Devices.Contains(e))
            {
                Devices.Remove(e);
            }
        }
        public void received_callback(Document doc)
        {
            var token = doc["Type"].ToString();
            var devices = doc["Devices"] as JObject;

            if (token != null && token == "response")
            {
                UpdateDeviceState((JArray)devices["Lights"], "Đèn");
                UpdateDeviceState((JArray)devices["Doors"], "Cửa");
                UpdateDeviceState((JArray)devices["Fans"], "Quạt");
                
                UpdateDevicesView();
            }
            //DataProvider.Ins.DB.SaveChanges();
            OnPropertyChanged(nameof(Devices));

        }
        public void UpdateDeviceState(JArray deviceState, string deviceType)
        {
            Application.Current.Dispatcher.Invoke(() => {
                foreach (JObject item in deviceState)
                {
                    int deviceIdEsp = item["id_esp"].Value<int>();
                    string status = item["status"].ToString();

                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(d => d.DeviceName == deviceIdEsp.ToString() && d.DeviceType == deviceType && d.Room.Id_esp32 == SelectedRoom.Id_esp32);
                    if (device != null)
                    {
                        var newDeviceState = new DeviceState
                        {
                            DeviceId = device.Id,
                            State = status == "on" ? "Bật" : "Tắt",
                            Timestamp = DateTime.Now
                        };
                        DataProvider.Ins.DB.DeviceState.Add(newDeviceState);
                    }
                }
                OnPropertyChanged(nameof(Devices));
                DataProvider.Ins.DB.SaveChanges();
            });
        }
        public void UpdateDevicesView()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var device in Devices)
                {
                    var latestState = DataProvider.Ins.DB.DeviceState
                        .Where(ds => ds.DeviceId == device.Id)
                        .OrderByDescending(ds => ds.Timestamp)
                        .FirstOrDefault();

                    if (latestState != null)
                    {
                        device.DeviceStateName = latestState.State;
                    }
                }
                OnPropertyChanged(nameof(Devices));
            });
        }
    }
}
