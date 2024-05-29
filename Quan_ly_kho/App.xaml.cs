using Quan_ly_kho.Models;
using Quan_ly_kho.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Quan_ly_kho
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private ScheduledTaskService _scheduledTaskService;

        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);

        //    // Khởi tạo Room và ScheduledTaskService
        //    var selectedRoom = new Room();
        //    _scheduledTaskService = new ScheduledTaskService(selectedRoom);

        //    // Thêm các lịch trình hiện có vào dịch vụ
        //    var existingSchedules = DataProvider.Ins.DB.Schedule.Include((s) => s.Device).ToList();
        //    foreach (var schedule in existingSchedules)
        //    {
        //        _scheduledTaskService.AddSchedule(schedule);
        //    }
        //}
    }
}
