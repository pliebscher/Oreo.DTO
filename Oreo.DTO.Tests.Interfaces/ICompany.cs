using System;
using System.Collections.Generic;
using System.Text;

namespace Oreo.DTO.Tests.Interfaces
{
    public interface ICompany
    {
        string Name { get; set; }
        IAddress Address { get; set; }
    }
}
