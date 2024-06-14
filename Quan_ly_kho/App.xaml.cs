using Quan_ly_kho.Models;
using Quan_ly_kho.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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
        private static StateManagementService _stateManagementService;
        public static StateManagementService StateManagementService => _stateManagementService;

        private static KeepAliveService _keepAliveService;
        public static KeepAliveService KeepAliveService => _keepAliveService;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Khởi tạo và kết nối Broker
            Broker.Instance.Connect();
            _stateManagementService = new StateManagementService();
            _keepAliveService = new KeepAliveService();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Ngắt kết nối Broker khi ứng dụng đóng
            Broker.Instance.Disconnect();


            base.OnExit(e);
        }

        
    }
}
