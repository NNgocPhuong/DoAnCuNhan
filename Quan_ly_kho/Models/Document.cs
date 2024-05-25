using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public partial class Document
    {
        public string ControlType { get => GetString(nameof(ControlType)); set => Push(nameof(ControlType), value); }
        public string Response {  get => GetString(nameof(Response)); set => Push(nameof(Response), value);}
        public string DeviceName {  get => GetString(nameof(DeviceName)); set => Push(nameof(DeviceName), value);}
        public string Power { get => GetString(nameof(Power)); set => Push(nameof(Power), value); }
    }
}
