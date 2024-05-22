﻿using Quan_ly_kho.Models;
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
        public event EventHandler<Device> DeviceEdited;
        public event EventHandler<Device> DeviceDeleted;
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
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand OnCommand { get; set; }
        public ICommand OffCommand { get; set; }
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
                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(x => x.DeviceName == DeviceName && x.RoomId == SelectedRoom.Id);
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
                            IsSelected = true,
                        };
                        var newDeviceState = new DeviceState()
                        {
                            DeviceId = newDevice.Id,
                            State = "Tắt"
                        };
                        newDevice.DeviceState.Add(newDeviceState);
                        // Thêm thiết bị mới vào cơ sở dữ liệu
                        DataProvider.Ins.DB.Device.Add(newDevice);

                        // Lưu thay đổi
                        DataProvider.Ins.DB.SaveChanges();

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
            OnCommand = new RelayCommand<object>(
                (p) => SelectedDevices.Count > 0,
                (p) =>
                {
                    var topic = "ESP32/20203535";
                    Broker = new Broker();
                    Broker.Connect();
                    foreach (var device in SelectedDevices)
                    {
                        device.ControlType = "on";
                        Document doc = new Document()
                        {
                            Id = device.Id,
                            ControlType = device.ControlType,
                        };
                        Broker.Send(topic, doc);
                    }
                });
        }

    }
}
