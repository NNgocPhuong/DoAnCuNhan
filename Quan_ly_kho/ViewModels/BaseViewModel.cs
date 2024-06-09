﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Quan_ly_kho.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private static bool _brokerConnected = false;
        private static readonly object _lock = new object();
        //private static Broker broker;
        //public static Broker Broker
        //{
        //    get => broker; 
        //    set
        //    {
        //        if (broker == null)
        //            broker = new Broker();
        //        broker = value;
        //    }
        //}
        public BaseViewModel() 
        {
            //Broker = new Broker();
            EnsureBrokerConnected();
        }
        private void EnsureBrokerConnected()
        {
            lock (_lock)
            {
                if (!_brokerConnected)
                {
                    Broker.Instance.Connect();
                    _brokerConnected = true;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    class RelayCommand<T> : ICommand
    {
        private readonly Predicate<T> _canExecute;
        private readonly Action<T> _execute;

        public RelayCommand(Predicate<T> canExecute, Action<T> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _canExecute = canExecute;
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            try
            {
                return _canExecute == null ? true : _canExecute((T)parameter);
            }
            catch
            {
                return true;
            }
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
