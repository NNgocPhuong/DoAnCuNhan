using Quan_ly_kho.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Quan_ly_kho.Views
{
    /// <summary>
    /// Interaction logic for BuildingScheduleWindow.xaml
    /// </summary>
    public partial class BuildingScheduleWindow : Window
    {
        public BuildingScheduleWindow(BuildingScheduleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
