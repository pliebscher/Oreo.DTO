using System;
using System.Collections.Generic;
using System.Text;

namespace Oreo.DTO.Tests.Interfaces
{
    public interface IAddress
    {
        string Line1 { get; set; }
        string Line2 { get; set; }
        string Zip { get; set; }
        IRegion Region { get; set; }
    }
}
