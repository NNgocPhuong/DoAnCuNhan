using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace Quan_ly_kho.ViewModels
{
    public class ScheduleViewModel : BaseViewModel
    {
        private System.Timers.Timer _timer;
        //private ScheduledTaskService _scheduledTaskService;
        //private ModifyViewModel _modifyViewModel;
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

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
                UpdateStartTime();
            }
        }

        private DateTime? _startTime;
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged(nameof(StartTime));
                UpdateStartTime();
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
                UpdateEndTime();
            }
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged(nameof(EndTime));
                UpdateEndTime();
            }
        }

        private DateTime? _startDateTime;
        public DateTime? StartDateTime
        {
            get => _startDateTime;
            set
            {
                _startDateTime = value;
                OnPropertyChanged(nameof(StartDateTime));
            }
        }

        private DateTime? _endDateTime;
        public DateTime? EndDateTime
        {
            get => _endDateTime;
            set
            {
                _endDateTime = value;
                OnPropertyChanged(nameof(EndDateTime));
            }
        }
        public ICommand DeleteScheduleCommand { get; set; }
        public ICommand AddCommand { get; set; }
        public ScheduleViewModel(Room selected_Room)
        {
            SelectedDevices = new ObservableCollection<Device>();
            SelectedRoom = selected_Room;
            //_scheduledTaskService = scheduledTaskService;

            //_timer = new Timer(60000); // Kiểm tra mỗi phút
            //_timer.Elapsed += OnTimerElapsed;
            //_timer.Start();


            AddCommand = new RelayCommand<object>(
        (p) =>
        {
            if (SelectedDevices.Count > 0 && StartDateTime.HasValue && EndDateTime.HasValue && StartDateTime < EndDateTime)
            {
                Schedule temp = new Schedule()
                {
                    StartTime = StartDateTime.Value,
                    EndTime = EndDateTime.Value,
                    Action = "Bật"
                };

                foreach (var device in SelectedDevices)
                {
                    foreach (var existingSchedule in device.Schedule)
                    {
                        if (IsOverlapping(existingSchedule, temp))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        },
        (p) =>
        {
            foreach (var device in SelectedDevices)
            {
                Schedule newSchedule = new Schedule
                {
                    StartTime = StartDateTime.Value,
                    EndTime = EndDateTime.Value,
                    Action = "Bật"
                };
                device.Schedule.Add(newSchedule);
                //DataProvider.Ins.DB.Schedule.Add(newSchedule);
                //_scheduledTaskService.AddSchedule(newSchedule);
            }
            DataProvider.Ins.DB.SaveChanges();
            
        });

            DeleteScheduleCommand = new RelayCommand<Schedule>((p) => true, (p) => { ExecuteDeleteSchedule(p); });
        }
        private void ExecuteDeleteSchedule(Schedule schedule)
        {
            foreach (var device in SelectedDevices)
            {
                if (device.Schedule.Contains(schedule))
                {
                    device.Schedule.Remove(schedule);
                    DataProvider.Ins.DB.Schedule.Remove(schedule);
                    //_scheduledTaskService.RemoveSchedule(schedule);
                }
            }
            DataProvider.Ins.DB.SaveChanges();
            OnPropertyChanged(nameof(SelectedDevices)); 
        }
        private bool IsOverlapping(Schedule existingSchedule, Schedule newSchedule)
        {
            return (newSchedule.StartTime <= existingSchedule.EndTime && newSchedule.EndTime >= existingSchedule.StartTime);
        }
        private void UpdateStartTime()
        {
            if (StartDate.HasValue && StartTime.HasValue)
            {
                StartDateTime = new DateTime(StartDate.Value.Year, StartDate.Value.Month, StartDate.Value.Day, StartTime.Value.Hour, StartTime.Value.Minute, StartTime.Value.Second);
            }
        }
        private void UpdateEndTime()
        {
            if (EndDate.HasValue && EndTime.HasValue)
            {
                EndDateTime = new DateTime(EndDate.Value.Year, EndDate.Value.Month, EndDate.Value.Day, EndTime.Value.Hour, EndTime.Value.Minute, EndTime.Value.Second);
            }
        }
       
    }
}
