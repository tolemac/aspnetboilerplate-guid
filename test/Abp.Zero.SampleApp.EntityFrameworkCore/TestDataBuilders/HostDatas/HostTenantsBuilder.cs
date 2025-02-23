using System;
using System.Linq;
using Abp.Extensions;
using Abp.Runtime.Security;

namespace Abp.Zero.SampleApp.EntityFrameworkCore.TestDataBuilders.HostDatas
{
    public class HostTenantsBuilder
    {
        private readonly AppDbContext _context;

        public HostTenantsBuilder(AppDbContext context)
        {
            _context = context;
        }

        public void Build()
        {
            CreateTenants();
        }

        private void CreateTenants()
        {
            CreateTenantIfNotExists(MultiTenancy.Tenant.DefaultTenantId, MultiTenancy.Tenant.DefaultTenantName);
        }

        private void CreateTenantIfNotExists(Guid id, string tenancyName)
        {
            if (_context.Tenants.Any(t => t.TenancyName == tenancyName && t.Id == id))
            {
                return;
            }

            var tenant = _context.Tenants.FirstOrDefault(t => t.TenancyName == tenancyName && t.Id == id);
            if (tenant == null)
            {
                tenant = _context.Tenants.Add(
                    new MultiTenancy.Tenant(id, tenancyName, tenancyName)
                    {
                        ConnectionString = SimpleStringCipher.Instance.Encrypt(
                            $"server=localhost;database=AbpZeroTenantDb_{tenancyName}_{Guid.NewGuid().ToString("N").Left(8)};trusted_connection=true;"
                        )
                    }).Entity;

                _context.SaveChanges();
            }
        }
    }
}