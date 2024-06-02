﻿using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Quan_ly_kho.ViewModels
{
    public class BuildingScheduleViewModel : BaseViewModel
    {
        private Building _selectedBuilding;
        public Building SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                _selectedBuilding = value;
                OnPropertyChanged();
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
        public BuildingScheduleViewModel(Building selected_Building)
        {
            SelectedBuilding = selected_Building;
            var DeviceList = DataProvider.Ins.DB.Building
                .Where(b => b.Id == SelectedBuilding.Id)
                .SelectMany(b => b.Floor)
                .SelectMany(b => b.Room)
                .SelectMany(b => b.Device)
                .Include(d => d.Schedule)
                .ToList();
            AddCommand = new RelayCommand<object>(
            (p) =>{
            if (DeviceList.Count > 0 && StartDateTime.HasValue && EndDateTime.HasValue && StartDateTime < EndDateTime)
            {
                Schedule temp = new Schedule()
                {
                    StartTime = StartDateTime.Value,
                    EndTime = EndDateTime.Value,
                    Action = "Bật"
                };

                foreach (var device in DeviceList)
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
            return false;},
            (p) =>
            {
            foreach (var device in DeviceList)
            {
                Schedule newSchedule = new Schedule
                {
                    StartTime = StartDateTime.Value,
                    EndTime = EndDateTime.Value,
                    Action = "Bật"
                };
                device.Schedule.Add(newSchedule);
                DataProvider.Ins.DB.Schedule.Add(newSchedule);
                //_scheduledTaskService.AddSchedule(newSchedule);
            }
            DataProvider.Ins.DB.SaveChanges();

            });

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