using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace Quan_ly_kho.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand ManageWindowCommand { get; set; }
        public bool Isloaded { get; set; } = false;
        public MainViewModel()
        {
            LoadedWindowCommand = new RelayCommand<Window>((p) => { return true; },
                (p) =>
                {
                    Isloaded = true;
                    if (p == null)
                        return;
                    p.Hide();
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();

                    if (loginWindow.DataContext == null)
                    {
                        return;
                    }
                    var LoginVM = loginWindow.DataContext as LoginViewModel;
                    if (LoginVM.IsLogin)
                    {
                        p.Show();
                    }
                    else
                    {
                        p.Close();
                    }
                });

            ManageWindowCommand = new RelayCommand<object>((p) => { return true; }, (p) => { ManageWindow w = new ManageWindow(); w.ShowDialog(); });
        }
    }
}
