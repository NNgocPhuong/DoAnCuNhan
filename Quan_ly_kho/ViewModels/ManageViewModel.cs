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

        public ManageViewModel()
        {
            Devices = new ObservableCollection<Device>();
            ModifyWindowCommand = new RelayCommand<object>((p) => { return true; }, (p) => { ModifyWindow w = new ModifyWindow(); w.ShowDialog(); });
        }
    }
}
