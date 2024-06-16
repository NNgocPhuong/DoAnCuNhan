using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Collections.ObjectModel;
using Quan_ly_kho.Models;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Quan_ly_kho.Views;
using System.Threading;

namespace Quan_ly_kho.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Các biến
        public ICommand ScheduleBuildingCommand { get; set; }
        private ObservableCollection<Building> _buildings;
        public ObservableCollection<Building> Buildings
        {
            get => _buildings;
            set { _buildings = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Floor> _floors;
        public ObservableCollection<Floor> Floors
        {
            get => _floors;
            set { _floors = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Room> _rooms;
        public ObservableCollection<Room> Rooms
        {
            get => _rooms;
            set { _rooms = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Device> _devices;
        public ObservableCollection<Device> Devices
        {
            get => _devices;
            set { _devices = value; OnPropertyChanged(); }
        }

        private Building _selectedBuilding;
        public Building SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                _selectedBuilding = value;
                OnPropertyChanged();
                UpdateFloors();
                SelectedFloor = null;
                SelectedRoom = null;
            }
        }

        private Floor _selectedFloor;
        public Floor SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                _selectedFloor = value;
                OnPropertyChanged();
                UpdateRooms();
                SelectedRoom = null;
            }
        }

        private Room _selectedRoom;
        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged();
                UpdateDevices();
            }
        }

        public ICommand LoadedWindowCommand { get; set; }
        public ICommand ManageWindowCommand { get; set; }
        public bool IsLoaded { get; set; } = false;
        #endregion
        private SchedulerTaskService _schedulerTaskService;
        private Timer _timer;
        public MainViewModel()
        {
            _schedulerTaskService = new SchedulerTaskService();
            _schedulerTaskService.Start();
            _timer = new Timer(SendKeepAlive, null, 0, 60000);
            LoadedWindowCommand = new RelayCommand<Window>((p) => true, async (p) =>
            {
                IsLoaded = true;
                if (p == null)
                    return;
                p.Hide();
                var loginWindow = new LoginWindow();
                loginWindow.ShowDialog();
                if (loginWindow.DataContext is LoginViewModel loginVM && loginVM.IsLogin)
                {
                    p.Show();
                    await LoadBuildingDataAsync();
                }
                else
                {
                    p.Close();
                }
            });

            ManageWindowCommand = new RelayCommand<object>((p) => SelectedRoom != null, (p) =>
            {
                var devices = SelectedRoom.Device.Where(d => d.RoomId == SelectedRoom.Id).ToList();
                var manageViewModel = new ManageViewModel(SelectedRoom, SelectedBuilding)
                {
                    Devices = new ObservableCollection<Device>(devices)
                };
                var manageWindow = new ManageWindow(manageViewModel);
                
                manageWindow.Show();
            });

            ScheduleBuildingCommand = new RelayCommand<object>((p) => SelectedBuilding != null,
                (p) =>
                {
                    var buildingScheduleViewModel = new BuildingScheduleViewModel(SelectedBuilding);
                    BuildingScheduleWindow w = new BuildingScheduleWindow(buildingScheduleViewModel);
                    w.ShowDialog();
                });
        }

        private void SendKeepAlive(object state)
        {
            Document doc = new Document()
            {
                ControlType = "Keep alive for software"
            };
            Broker.Instance.Send("KeepAlive", doc);
        }

        public async Task LoadBuildingDataAsync()
        {
            var buildingList = await DataProvider.Ins.DB.Building.ToListAsync();
            Buildings = new ObservableCollection<Building>(buildingList);
        }

        public async void UpdateFloors()
        {
            if (SelectedBuilding != null)
            {
                var floorList = await DataProvider.Ins.DB.Floor.Where(x => x.BuildingId == SelectedBuilding.Id).ToListAsync();
                Floors = new ObservableCollection<Floor>(floorList);
            }
            else
            {
                Floors = new ObservableCollection<Floor>();
            }
        }

        public async void UpdateRooms()
        {
            if (SelectedFloor != null)
            {
                var roomList = await DataProvider.Ins.DB.Room.Where(x => x.FloorId == SelectedFloor.Id).ToListAsync();
                Rooms = new ObservableCollection<Room>(roomList);
            }
            else
            {
                Rooms = new ObservableCollection<Room>();
            }
        }

        public async void UpdateDevices()
        {
            if (SelectedRoom != null)
            {
                var deviceList = await DataProvider.Ins.DB.Device
                    .Where(x => x.RoomId == SelectedRoom.Id)
                    .Include(x => x.DeviceState)
                    .Include(x => x.Schedule)
                    .ToListAsync();
                Devices = new ObservableCollection<Device>(deviceList);
            }
            else
            {
                Devices = new ObservableCollection<Device>();
            }
        }
    }
}
