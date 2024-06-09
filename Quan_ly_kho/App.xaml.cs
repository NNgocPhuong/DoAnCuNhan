using Quan_ly_kho.Models;
using Quan_ly_kho.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Quan_ly_kho
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Timer _scheduleCleanupTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Khởi tạo và kết nối Broker
            Broker.Instance.Connect();

            // Khởi động bộ định thời để kiểm tra và xóa các lịch trình đã kết thúc
            _scheduleCleanupTimer = new Timer(RemoveExpiredSchedules, null, 0, 300000);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Ngắt kết nối Broker khi ứng dụng đóng
            Broker.Instance.Disconnect();

            // Dừng bộ định thời
            _scheduleCleanupTimer?.Dispose();

            base.OnExit(e);
        }

        private void RemoveExpiredSchedules(object state)
        {
            var now = DateTime.Now;

            // Find schedules that have already ended
            var expiredSchedules = DataProvider.Ins.DB.Schedule
                                    .Where(s => s.EndTime < now)
                                    .ToList();

            foreach (var schedule in expiredSchedules)
            {
                DataProvider.Ins.DB.Schedule.Remove(schedule);
            }

            // Save changes to the database
            DataProvider.Ins.DB.SaveChanges();

        }
    }
}
