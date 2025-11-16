using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Tenant
{
    internal static class Tenants
    {
        public static readonly Guid Tenant1 = new Guid("11223344-5566-7788-99AA-BBCCDDEEFF00");
        public static readonly Guid Tenant2 = new Guid("11223345-5566-7788-99AA-BBCCDDEEFF00");
        public static readonly Guid Tenant3 = new Guid("11223346-5566-7788-99AA-BBCCDDEEFF00");

        public static readonly Guid[] All = [Tenant1, Tenant2, Tenant3];
    }
}
