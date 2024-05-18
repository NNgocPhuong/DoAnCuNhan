using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace Quan_ly_kho.ViewModels
{
    public class ManageViewModel : BaseViewModel
    {
        public ICommand ModifyWindowCommand { get; set; }
        public bool Isloaded { get; set; } = false;
        public ManageViewModel()
        {
            ModifyWindowCommand = new RelayCommand<object>((p) => { return true; }, (p) => { ModifyWindow w = new ModifyWindow(); w.ShowDialog(); });
        }
    }
}
