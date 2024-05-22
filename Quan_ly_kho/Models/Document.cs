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
    }
}
