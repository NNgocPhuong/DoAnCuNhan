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

namespace Quan_ly_kho.ViewModels
{
    public class ManageViewModel : BaseViewModel
    {
        #region Khai báo biến
        public ICommand ModifyWindowCommand { get; set; }
        private ObservableCollection<Device> _devices;
        public ICommand ScheduleRoomCommand { get; set; }
        public ICommand ScheduleBuildingCommand { get; set; }
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
          
            string firstTopic = (SelectedRoom.Floor.Building.BuildingName.ToLower() + SelectedRoom.RoomNumber).ToMD5();
            
            if (SelectedRoom.Id_esp32 != null && SelectedRoom.Id_esp32 != "esp32/")
            {
                Document doc1 = new Document()
                {
                    Response = "received",
                };

                Broker.Send(firstTopic, doc1);
            }
            else
            {
                SelectedRoom.Id_esp32 = "esp32/";
                bool isSend = false;
                Broker.Listen(firstTopic, (doc) =>
                {
                    if (!isSend)
                    {
                        SelectedRoom.Id_esp32 += doc.ObjectId;
                        Broker.Unsubscribe(firstTopic);
                        Document doc1 = new Document()
                        {
                            Response = "received",
                        };

                        Broker.Send(firstTopic, doc1);
                        isSend = true;
                    }
                });
                UpdateRoomIdEsp32(SelectedRoom.Id, SelectedRoom.Id_esp32);
            }
            // Xoá các lịch trình đã thực hiện
            RemoveExpiredSchedules();
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
            ScheduleBuildingCommand = new RelayCommand<object>((p) => { return true; },
                (p) =>
                {
                    var buildingScheduleViewModel = new BuildingScheduleViewModel(SelectedBuilding);
                    BuildingScheduleWindow w = new BuildingScheduleWindow(buildingScheduleViewModel);
                    w.ShowDialog();
                });
        }
        private void RemoveExpiredSchedules()
        {
            var now = DateTime.Now;
            var expiredSchedules = DataProvider.Ins.DB.Schedule
                                    .Where(s => DbFunctions.TruncateTime(s.EndTime) < DbFunctions.TruncateTime(now))
                                    .ToList();

            foreach (var schedule in expiredSchedules)
            {
                DataProvider.Ins.DB.Schedule.Remove(schedule);
            }

            DataProvider.Ins.DB.SaveChanges();
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

    }
}
