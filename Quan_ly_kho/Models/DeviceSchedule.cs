using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quan_ly_kho.Models
{
    public class DeviceSchedule
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public DeviceSchedule(DateTime start_time, DateTime end_time) 
        {
            StartTime = start_time;
            EndTime = end_time;
        }
    }
}
