using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Quan_ly_kho.ViewModels
{
    public class ScheduleViewModel : BaseViewModel
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
        private Schedule _newSchedule;
        public Schedule NewSchedule
        {
            get => _newSchedule;
            set
            {
                _newSchedule = value;
                OnPropertyChanged(nameof(NewSchedule));
            }
        }
        public ICommand AddCommand { get; set; }
        public ScheduleViewModel(Room selected_Room)
        {
            SelectedDevices = new ObservableCollection<Device>();
            SelectedRoom = selected_Room;
            NewSchedule = new Schedule();
            AddCommand = new RelayCommand<object>(
                (p) => 
                {
                    if(SelectedDevices.Count > 0)
                    {
                        return true;
                    }
                    return false;
                },
                (p) =>
                {
                    foreach (var device in SelectedDevices)
                    {
                        device.Schedule.Add(new Schedule
                        {
                            StartTime = NewSchedule.StartTime,
                            EndTime = NewSchedule.EndTime,
                            Action = "Bật"
                        });
                    }
                });
        }
    }
}
