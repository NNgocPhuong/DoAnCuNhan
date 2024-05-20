﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Quan_ly_kho.Models;

namespace System
{
    partial class Document
    {
        public int RoomId { get => GetValue<int>(nameof(RoomId)); set => Push(nameof(RoomId), value); }
        public string DeviceName { get => GetString(nameof(DeviceName)); set => Push(nameof(DeviceName), value); }
        public string DeviceType { get => GetString(nameof(DeviceType)); set => Push(nameof(DeviceType), value); }
        public virtual Room Room { get => GetObject<Room>(nameof(Room)); set => Push(nameof(Room), value); }
    }
}

namespace Quan_ly_kho.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Device : Document
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Device()
        {
            this.DeviceState = new HashSet<DeviceState>();
            this.Schedule = new HashSet<Schedule>();
        }
    
      
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DeviceState> DeviceState { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Schedule> Schedule { get; set; }
    }
    public partial class Device
    {
        public string DeviceStateName
        {
            get
            {
                // Lấy trạng thái từ một trong các DeviceState (giả sử là trạng thái của thiết bị là trạng thái của DeviceState đầu tiên)
                if (DeviceState != null && DeviceState.Count > 0)
                {
                    // Chuyển ICollection thành danh sách List
                    var deviceStateList = DeviceState.ToList();

                    // Truy cập vào phần tử đầu tiên của danh sách
                    return deviceStateList[0].State;
                }
                return null;
            }
        }
    }
}
