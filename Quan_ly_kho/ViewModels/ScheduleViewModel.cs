﻿using Quan_ly_kho.Models;
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
        public ICommand AddCommand { get; set; }
        public ScheduleViewModel(Room selected_Room)
        {
            SelectedDevices = new ObservableCollection<Device>();
            SelectedRoom = selected_Room;

            AddCommand = new RelayCommand<object>(
        (p) =>
        {
            if (SelectedDevices.Count > 0 && StartDateTime.HasValue && EndDateTime.HasValue && StartDateTime < EndDateTime)
            {
                if(SelectedDevices.Count > 0)
                    return true;
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
                device.Schedule.Add(new Schedule
                {
                    StartTime = StartDateTime.Value,
                    EndTime = EndDateTime.Value,
                    Action = "Bật"
                });
            }
        });
        }
        private bool IsOverlapping(Schedule existingSchedule, Schedule newSchedule)
        {
            return (newSchedule.StartTime < existingSchedule.EndTime && newSchedule.EndTime > existingSchedule.StartTime);
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