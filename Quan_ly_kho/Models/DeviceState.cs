//------------------------------------------------------------------------------
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
        public int DeviceId { get => GetValue<int>(nameof(DeviceId)); set => Push(nameof(DeviceId), value); }
        public string State { get => GetString(nameof(State)); set => Push(nameof(State), value); }
        public virtual Device Device { get => GetObject<Device>(nameof(Device)); set => Push(nameof(Device), value); }
    }
}

namespace Quan_ly_kho.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DeviceState : Document
    {
        public Nullable<System.DateTime> Timestamp { get; set; }
    }
}
