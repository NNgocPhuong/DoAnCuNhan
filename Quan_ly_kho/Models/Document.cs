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
        public string Token { get => GetString(nameof(Token)); set => Push(nameof(Token), value); }
        public int[] Devices { get => GetArray<int[]>(nameof(Devices)); set => Push(nameof(Devices), value); }
        public string Status { get => GetString(nameof(Status)); set => Push(nameof(Status), value); }
        public string Command { get => GetString(nameof(Command)); set => Push(nameof(Command), value); }
        public string Type {  get => GetString(nameof(Type)); set => Push(nameof(Type), value);}
    }
}
