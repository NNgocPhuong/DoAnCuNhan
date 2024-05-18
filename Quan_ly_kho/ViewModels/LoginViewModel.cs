using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

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
        public LoginViewModel()
        {
            PassWord = "";
            UserName = "";
            IsLogin = false;
            LoginCommand = new RelayCommand<Window>((p) => { return true; }, (p) => { Login(p); });
            CloseCommand = new RelayCommand<Window>((p) => { return true; }, (p) => { p.Close(); });
            PasswordChangedCommand = new RelayCommand<PasswordBox>((p) => { return true; }, (p) => { PassWord = p.Password; });
        }

        void Login(Window p)
        {
            if (p == null)
                return;
            IsLogin = true;
            p.Close();
            //Users user = new Users(UserName, PassWord);
            //var accCount = DataProvider.Ins.DB.Users.Where(u => u.UserName == UserName && u.Password == PassWord).Count();
            //if (accCount > 0)
            //{
            //    IsLogin = true;
            //    p.Close();
            //}
            //else
            //{
            //    IsLogin = false;
            //    MessageBox.Show("Tài khoản hoặc mật khẩu sai");
            //}
        }
    }
}
