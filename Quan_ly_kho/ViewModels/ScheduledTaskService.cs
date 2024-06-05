using Newtonsoft.Json.Linq;
using Quan_ly_kho.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Quan_ly_kho.ViewModels
{
    public class ScheduledTaskService
    {
        private readonly Timer _timer;

        public ScheduledTaskService()
        {
            _timer = new Timer(60000); // Check every minute
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var schedulesToRemove = DataProvider.Ins.DB.Schedule.Where(s => s.EndTime < now).ToList();

            foreach (var schedule in schedulesToRemove)
            {
                DataProvider.Ins.DB.Schedule.Remove(schedule);
            }

            DataProvider.Ins.DB.SaveChanges();
        }

        public void AddSchedule(Schedule schedule)
        {
            DataProvider.Ins.DB.Schedule.Add(schedule);
            DataProvider.Ins.DB.SaveChanges();
        }

        public void RemoveSchedule(Schedule schedule)
        {
            DataProvider.Ins.DB.Schedule.Remove(schedule);
            DataProvider.Ins.DB.SaveChanges();
        }
    }
}

