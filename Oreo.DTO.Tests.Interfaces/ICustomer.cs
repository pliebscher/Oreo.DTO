using System;

namespace Oreo.DTO.Tests.Interfaces
{    public interface ICustomer
    {
        int Id { get; set; }
        string LastName { get; set; }
        string FirstName { get; set; }
        IAddress Address { get; set; }
        ICompany Company { get; set; }
        DateTime LastModified { get; set; }
    }
}
