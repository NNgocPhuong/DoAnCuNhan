using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quan_ly_kho.ViewModels
{
    public class ModifyViewModel : BaseViewModel
    {
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
                if(SelectedDevice != null)
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
        public ModifyViewModel()
        {
            SelectedDevices = new ObservableCollection<Device>();
        }
    }
}
