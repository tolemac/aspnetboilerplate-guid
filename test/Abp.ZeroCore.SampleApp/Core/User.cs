using Abp.Authorization.Users;
using System;

namespace Abp.ZeroCore.SampleApp.Core
{
    public class User : AbpUser<User>
    {
        public override string ToString()
        {
            return string.Format("[User {0}] {1}", Id, UserName);
        }

        public static User CreateTenantAdminUser(Guid tenantId, string emailAddress)
        {
            var user = new User
            {
                TenantId = tenantId,
                UserName = AdminUserName,
                Name = AdminUserName,
                Surname = AdminUserName,
                EmailAddress = emailAddress
            };

            user.SetNormalizedNames();

            return user;
        }
    }
}