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

namespace Quan_ly_kho.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private ObservableCollection<Building> _buildings;
        public ObservableCollection<Building> Buildings { get => _buildings; set { _buildings = value; OnPropertyChanged(); } }

        private ObservableCollection<Floor> _floors;
        public ObservableCollection<Floor> Floors { get => _floors; set { _floors = value; OnPropertyChanged(); } }
        
        private ObservableCollection<Room> _rooms;
        public ObservableCollection<Room> Rooms { get => _rooms; set { _rooms = value; OnPropertyChanged(); } }

        private ObservableCollection<Device> _devices;
        public ObservableCollection<Device> Devices { get => _devices; set { _devices = value; OnPropertyChanged(); } }

        private Building _selectedBuilding;
        private Floor _selectedFloor;
        private Room _selectedRoom;

        public Building SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                _selectedBuilding = value;
                OnPropertyChanged();
                UpdateFloor();
                SelectedFloor = null;
                SelectedRoom = null;
            }
        }
        public Floor SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                _selectedFloor = value;
                OnPropertyChanged();
                UpdateRoom();
                SelectedRoom = null;
            }
        }

        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                UpdateDevice();
                OnPropertyChanged();
            }
        }
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand ManageWindowCommand { get; set; }
        public bool Isloaded { get; set; } = false;
        public MainViewModel()
        {
            LoadedWindowCommand = new RelayCommand<Window>((p) => { return true; },
                (p) =>
                {
                    Isloaded = true;
                    if (p == null)
                        return;
                    p.Hide();
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();

                    if (loginWindow.DataContext == null)
                    {
                        return;
                    }
                    var LoginVM = loginWindow.DataContext as LoginViewModel;
                    if (LoginVM.IsLogin)
                    {
                        p.Show();
                        LoadBuildingData();
                    }
                    else
                    {
                        p.Close();
                    }
                });

            ManageWindowCommand = new RelayCommand<object>(
                (p) =>
                {
                    if(SelectedRoom != null)
                        return true;
                    return false;
                }, 
                (p) => 
                { 
                    ManageWindow w = new ManageWindow();
                    if (w.DataContext is ManageViewModel manageViewModel)
                    {
                        manageViewModel.Devices = Devices;
                        manageViewModel.SelectedRoom = SelectedRoom;
                    }
                    w.ShowDialog();
                });
        }
        public void LoadBuildingData()
        {
            Buildings = new ObservableCollection<Building> { };
            
            var BuildingList = DataProvider.Ins.DB.Building.ToList();
            foreach(var item in BuildingList)
            {
                Buildings.Add(item);
            }
            
        }

        public void UpdateFloor()
        {
            Floors = new ObservableCollection<Floor> { };
            Floors.Clear();
            if (SelectedBuilding != null)
            {
                var FloorList = DataProvider.Ins.DB.Floor.Where(x => x.BuildingId == SelectedBuilding.Id);
                foreach (var item in FloorList)
                {
                    Floors.Add(item);
                }
            }
        }
        public void UpdateRoom()
        {
            Rooms = new ObservableCollection<Room> { };
            Rooms.Clear();
            if (SelectedFloor != null)
            {
                var RoomList = DataProvider.Ins.DB.Room.Where(x => x.FloorId == SelectedFloor.Id);
                foreach (var item in RoomList)
                {
                    Rooms.Add(item);
                }
            }
        }
        public void UpdateDevice()
        {
            Devices = new ObservableCollection<Device> { };
            if (SelectedRoom != null)
            {
                var DeviveList = DataProvider.Ins.DB.Device.Where(x => x.RoomId == SelectedRoom.Id).Include(x => x.DeviceState).Include(x => x.Schedule).ToList();
                Devices = new ObservableCollection<Device>(DeviveList);
            }
            else
            {
                // Nếu SelectedRoom là null, chỉ tạo một ObservableCollection rỗng
                Devices = new ObservableCollection<Device>();
            }
        }
    }
}
