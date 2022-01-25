using NUnit.Framework;

using Oreo.DTO.Tests.Interfaces;

namespace Oreo.DTO.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            IRegion region = DTO.Create<IRegion>();

            Assert.IsNotNull(region);
        }

        [Test]
        public void Test2()
        {
            ICompany company = DTO.Create<ICompany>();

            Assert.IsNotNull(company);

        }

        [Test]
        public void Test3()
        {
            ICustomer customer = DTO.Create<ICustomer>();


            //this is a comment
            Assert.IsNotNull(customer);
            //this is also a comment
            Assert.IsNotNull(customer.Address);
            //added a comment in test-branch-1

        }

        [Test]
        public void Test4()
        {
            IAddress address = DTO.Create<IAddress>();

            Assert.IsNotNull(address);

        }
    }
}