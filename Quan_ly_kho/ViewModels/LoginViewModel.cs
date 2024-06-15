using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Quan_ly_kho.Models;
using System.Threading;

namespace Quan_ly_kho.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public bool IsLogin { get; set; }
        private string _username;
        public string UserName { get { return _username; } set { _username = value; OnPropertyChanged(); } }
        private string _password;
        public string PassWord { get { return _password; } set { _password = value; OnPropertyChanged(); } }
        public ICommand LoginCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand PasswordChangedCommand { get; set; }
        private Timer _scheduleCleanupTimer;
        private static readonly object _lockObject = new object();
        public LoginViewModel()
        {
            PassWord = "";
            UserName = "";
            IsLogin = false;
            LoginCommand = new RelayCommand<Window>((p) => { return true; }, (p) => { Login(p); });
            CloseCommand = new RelayCommand<Window>((p) => { return true; }, (p) => { p.Close(); });
            PasswordChangedCommand = new RelayCommand<PasswordBox>((p) => { return true; }, (p) => { PassWord = p.Password; });
            // Khởi động bộ định thời để kiểm tra và xóa các lịch trình đã kết thúc
            _scheduleCleanupTimer = new Timer(RemoveExpiredSchedules, null, 0, 60000);
        }

        void Login(Window p)
        {
            if (p == null)
                return;
            IsLogin = true;

            User user = new User(UserName, PassWord);
            var accCount = DataProvider.Ins.DB.User.Where(u => u.Username == UserName && u.PasswordHash == PassWord).Count();
            if (accCount > 0)
            {
                IsLogin = true;
                p.Close();
            }
            else
            {
                IsLogin = false;
                MessageBox.Show("Tài khoản hoặc mật khẩu sai");
            }
        }
        private void RemoveExpiredSchedules(object state)
        {
            lock (_lockObject)
            {
                var now = DateTime.Now;
                var thresholdTime = now.AddMinutes(-2); // Tính toán thời gian ngưỡng trước khi truy vấn
                // Tìm các ID của lịch trình đã kết thúc
                var expiredScheduleIds = DataProvider.Ins.DB.Schedule
                                        .Where(s => s.EndTime <= thresholdTime)
                                        .Select(s => s.Id) 
                                        .ToList();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var id in expiredScheduleIds)
                    {
                        var schedule = DataProvider.Ins.DB.Schedule.Find(id);
                        if (schedule != null)
                        {
                            DataProvider.Ins.DB.Schedule.Remove(schedule);
                        }
                    }
                    // Lưu các thay đổi vào cơ sở dữ liệu
                    DataProvider.Ins.DB.SaveChanges();

                });
            }
        }
    }
}
