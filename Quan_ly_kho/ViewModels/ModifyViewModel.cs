using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Quan_ly_kho.ViewModels
{
    public class ModifyViewModel : BaseViewModel
    {
        public event EventHandler<Device> DeviceAdded;
        private ObservableCollection<Device> _selectedDevices;

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
        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged(nameof(SelectedRoom));
            }
        }
        public ICommand AddCommand { get; set; }
        public ModifyViewModel()
        {  
            SelectedDevices = new ObservableCollection<Device>();
            SelectedRoom = new Room();
            AddCommand = new RelayCommand<object>(
                (p) =>
                {
                    if (string.IsNullOrEmpty(DeviceName))
                    {
                        return false;
                    }
                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(x => x.DeviceName == DeviceName);
                    if (device != null)
                        return false;

                    return true;
                },
                (p) =>
                {
                    var room = DataProvider.Ins.DB.Room.FirstOrDefault(r => r.Id == SelectedRoom.Id);

                    if (room != null)
                    {
                        var newDevice = new Device()
                        {
                            DeviceName = DeviceName,
                            DeviceType = DeviceType,
                            RoomId = room.Id,
                        };

                       
                        // Thêm thiết bị mới vào cơ sở dữ liệu
                        DataProvider.Ins.DB.Device.Add(newDevice);

                        // Lưu thay đổi
                        DataProvider.Ins.DB.SaveChanges();

                        // Cập nhật danh sách thiết bị được chọn
                        SelectedDevices.Add(newDevice);
                        DeviceAdded?.Invoke(this, newDevice);
                    }
                });
        } 
       
    }
}
