﻿using System.Linq;
using Abp.Configuration.Startup;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.TestBase.SampleApplication.ContactLists;
using Shouldly;
using Xunit;

namespace Abp.TestBase.SampleApplication.Tests.ContactLists
{
    public class ContactList_MultiTenancy_Tests : SampleApplicationTestBase
    {
        private readonly IRepository<ContactList> _contactListRepository;
        private readonly IContactListAppService _contactListAppService;

        public ContactList_MultiTenancy_Tests()
        {
            Resolve<IMultiTenancyConfig>().IsEnabled = true;
            _contactListRepository = Resolve<IRepository<ContactList>>();
            _contactListAppService = Resolve<IContactListAppService>();
        }

        [Fact]
        public void MustHaveTenant_Filter_Tests()
        {
            AbpSession.UserId = GuidExtensions.Guid1;

            //A tenant can reach its own data
            AbpSession.TenantId = GuidExtensions.Guid1;
            _contactListRepository.GetAllList().Any(cl => cl.TenantId != AbpSession.TenantId).ShouldBe(false);

            //A tenant can reach its own data
            AbpSession.TenantId = GuidExtensions.Guid2;
            _contactListRepository.GetAllList().Any(cl => cl.TenantId != AbpSession.TenantId).ShouldBe(false);

            //Tenant 999999 has no data
            AbpSession.TenantId = GuidExtensions.Guid999999;
            _contactListRepository.GetAllList().Count.ShouldBe(0);

            //Host can reach all tenant data (since MustHaveTenant filter is disabled for host as default)
            AbpSession.TenantId = null;
            _contactListRepository.GetAllList().Count.ShouldBe(4);

            //Host can filter tenant data if it wants
            _contactListRepository.GetAllList().Count(t => t.TenantId == GuidExtensions.Guid1).ShouldBe(1);
            _contactListRepository.GetAllList().Count(t => t.TenantId == GuidExtensions.Guid2).ShouldBe(1);
            _contactListRepository.GetAllList().Count(t => t.TenantId == GuidExtensions.Guid999999).ShouldBe(0);

            //We can also set tenantId parameter's value without changing AbpSession.TenantId
            var unitOfWorkManager = Resolve<IUnitOfWorkManager>();
            using (var unitOfWork = unitOfWorkManager.Begin())
            {
                //Host can reach all tenant data (since MustHaveTenant filter is disabled for host as default)
                _contactListRepository.GetAllList().Count.ShouldBe(4);

                using (unitOfWorkManager.Current.EnableFilter(AbpDataFilters.MustHaveTenant))
                {
                    //We can not get any entity since filter is enabled (even we're host)
                    _contactListRepository.GetAllList().Count.ShouldBe(0);

                    //We're overriding filter parameter's value
                    unitOfWorkManager.Current.SetTenantId(GuidExtensions.Guid1);

                    //We should only get tenant 1's entities since we set tenantId to 1
                    var contactLists = _contactListRepository.GetAllList();
                    contactLists.Count.ShouldBe(1);
                    contactLists.Any(cl => cl.TenantId != GuidExtensions.Guid1).ShouldBe(false);
                }

                unitOfWork.Complete();
            }
        }

        [Fact]
        public void Setting_SetTenantId_Should_Enable_Or_Disable_MustHaveTenant_Filter()
        {
            AbpSession.TenantId = null;

            var unitOfWorkManager = Resolve<IUnitOfWorkManager>();
            using (var unitOfWork = unitOfWorkManager.Begin())
            {
                //Host can reach all tenant data (since MustHaveTenant filter is disabled for host as default)
                _contactListRepository.GetAllList().Count.ShouldBe(4);

                unitOfWorkManager.Current.SetTenantId(GuidExtensions.Guid1);
                //We should only get tenant 1's entities since we set tenantId to 1 (which automatically enables MustHaveTenant filter)
                var contactLists = _contactListRepository.GetAllList();
                contactLists.Count.ShouldBe(1);
                contactLists.Any(cl => cl.TenantId != GuidExtensions.Guid1).ShouldBe(false);

                unitOfWorkManager.Current.SetTenantId(null);
                //Switched to host, which automatically disables MustHaveTenant filter
                _contactListRepository.GetAllList().Count.ShouldBe(4);

                unitOfWork.Complete();
            }
        }

        [Fact]
        public void MustHaveTenant_Should_Work_In_AppService()
        {
            AbpSession.TenantId = GuidExtensions.Guid3;
            AbpSession.UserId = GuidExtensions.Guid3;

            var lists = _contactListAppService.GetContactLists();
            lists.Count.ShouldBeGreaterThan(0);
        }
    }
}
