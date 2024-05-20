using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Quan_ly_kho.Models;
using System.Collections.ObjectModel;

namespace Quan_ly_kho.ViewModels
{
    public class ManageViewModel : BaseViewModel
    {
        public ICommand ModifyWindowCommand { get; set; }
        private ObservableCollection<Device> _devices;

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

        private void SelectAllDevices(bool isSelected)
        {
            foreach(var item in Devices)
            {
                item.IsSelected = isSelected;
            }
        }

        public ManageViewModel()
        {
            Devices = new ObservableCollection<Device>();
            ModifyWindowCommand = new RelayCommand<object>((p) => { return true; },
                (p) => 
                { 
                    var selectedDevices = Devices.Where(d => d.IsSelected).ToList();
                    ModifyWindow w = new ModifyWindow();
                    if (w.DataContext is ModifyViewModel modifyViewModel)
                    {
                        modifyViewModel.SelectedDevices = new ObservableCollection<Device>(selectedDevices);
                    }
                    w.ShowDialog();
                });
        }
    }
}
