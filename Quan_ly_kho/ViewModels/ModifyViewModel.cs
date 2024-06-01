﻿using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
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
            get => _selectedDevice;
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
        public string DeviceName
        {
            get => _deviceName;
            set
            {
                _deviceName = value;
                OnPropertyChanged();
            }
        }

        private string _deviceType;
        public string DeviceType
        {
            get => _deviceType;
            set
            {
                _deviceType = value;
                OnPropertyChanged();
            }
        }

        private string _deviceState;
        public string DeviceState
        {
            get => _deviceState;
            set
            {
                _deviceState = value;
                OnPropertyChanged();
            }
        }

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

        public ModifyViewModel(Room selectedRoom)
        {
            SelectedDevices = new ObservableCollection<Device>();
            SelectedRoom = selectedRoom;

            AddCommand = new RelayCommand<object>(
                (p) => !string.IsNullOrEmpty(DeviceName) && DataProvider.Ins.DB.Device.FirstOrDefault(x => x.DeviceName == DeviceName && x.RoomId == SelectedRoom.Id && x.DeviceType == DeviceType) == null,
                async (p) =>
                {
                    var room = await DataProvider.Ins.DB.Room.FirstOrDefaultAsync(r => r.Id == SelectedRoom.Id);
                    if (room != null)
                    {
                        var newDevice = new Device
                        {
                            DeviceName = DeviceName,
                            DeviceType = DeviceType,
                            RoomId = room.Id,
                            IsSelected = true,
                            DeviceState = new ObservableCollection<DeviceState> { new DeviceState { State = "Tắt" } }
                        };
                        DataProvider.Ins.DB.Device.Add(newDevice);
                        await SaveChangesAsync();
                        SelectedDevices.Add(newDevice);
                        DeviceAdded?.Invoke(this, newDevice);
                    }
                });

            EditCommand = new RelayCommand<object>(
                (p) => !string.IsNullOrEmpty(DeviceName) && SelectedDevice != null && DataProvider.Ins.DB.Device.FirstOrDefault(x => x.DeviceName == DeviceName && x.RoomId == SelectedRoom.Id && x.DeviceType == DeviceType) == null,
                (p) =>
                {
                    var device = DataProvider.Ins.DB.Device.FirstOrDefault(d => d.Id == SelectedDevice.Id);
                    if (device != null)
                    {
                        device.DeviceName = DeviceName;
                        device.DeviceType = DeviceType;
                        DataProvider.Ins.DB.SaveChanges();
                        DeviceEdited?.Invoke(this, device);
                    }
                });

            DeleteCommand = new RelayCommand<object>(
                (p) => SelectedDevice != null,
                (p) =>
                {
                    var device = DataProvider.Ins.DB.Device.Include(x => x.DeviceState).Include(x => x.Schedule).SingleOrDefault(d => d.Id == SelectedDevice.Id);
                    if (device != null)
                    {
                        DataProvider.Ins.DB.DeviceState.RemoveRange(device.DeviceState);
                        DataProvider.Ins.DB.Schedule.RemoveRange(device.Schedule);
                        DataProvider.Ins.DB.Device.Remove(device);
                        DataProvider.Ins.DB.SaveChanges();
                        SelectedDevices.Remove(device);
                        DeviceDeleted?.Invoke(this, device);
                    }
                });

            OnCommand = new RelayCommand<object>(
                (p) => SelectedDevices.Count > 0,
                async (p) => await SendControlMessage("on"));

            OffCommand = new RelayCommand<object>(
                (p) => SelectedDevices.Count > 0,
                async (p) => await SendControlMessage("off"));
        }

        private async Task SendControlMessage(string command)
        {
            var controlMessage = new JObject
            {
                { "Type", "control" },
                { "Devices", CreateDevicesObject(command) }
            };
            Broker.Send(SelectedRoom.Id_esp32, controlMessage.ToString());

            await ListenForResponseAndUpdateState(command);
        }

        private JObject CreateDevicesObject(string command)
        {
            var devicesObject = new JObject
            {
                { "Doors", new JArray(SelectedDevices.Where(d => d.DeviceType.StartsWith("Cửa")).Select(d => new JObject { { "id_esp", d.DeviceName }, { "power", command } })) },
                { "Lights", new JArray(SelectedDevices.Where(d => d.DeviceType.StartsWith("Đèn")).Select(d => new JObject { { "id_esp", d.DeviceName }, { "power", command } })) },
                { "Fans", new JArray(SelectedDevices.Where(d => d.DeviceType.StartsWith("Quạt")).Select(d => new JObject { { "id_esp", d.DeviceName }, { "power", command } })) },
                { "AirConditionings", new JArray(SelectedDevices.Where(d => d.DeviceType.StartsWith("Điều hoà")).Select(d => new JObject { { "id_esp", d.DeviceName }, { "power", command } })) }
            };
            return devicesObject;
        }

        private async Task ListenForResponseAndUpdateState(string command)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var tcs = new TaskCompletionSource<bool>();

            Action<Document> handler = null;
            handler = (doc) =>
            {
                if (doc["Type"]?.ToString() == "ack-control" && doc["Response"]?.ToString() == "control-success")
                {
                    tcs.TrySetResult(true);
                    Broker.process_received_data -= handler; // Unsubscribe after handling
                }
            };

            Broker.process_received_data += handler;
            Broker.Listen(SelectedRoom.Id_esp32, handler);
            try
            {
                var result = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));

                if (result == tcs.Task && tcs.Task.Result)
                {
                    UpdateDeviceStates(command == "on" ? "Bật" : "Tắt");
                }
                else
                {
                    UpdateDeviceStates("Lỗi");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions (log them if necessary)
                UpdateDeviceStates("Lỗi");
            }
            finally
            {
                // Ensure handler is unsubscribed if Task was not completed successfully
                Broker.process_received_data -= handler;
            }
        }


        private void UpdateDeviceStates(string state)
        {
            foreach (var device in SelectedDevices)
            {
                device.DeviceState.Add(new DeviceState
                {
                    DeviceId = device.Id,
                    State = state
                });
                DeviceEdited?.Invoke(this, device);
            }
            DataProvider.Ins.DB.SaveChanges();
            OnPropertyChanged(nameof(SelectedDevices));
        }

        private async Task SaveChangesAsync()
        {
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
        }
    }
}
