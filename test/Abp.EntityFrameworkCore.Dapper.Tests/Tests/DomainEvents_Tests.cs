﻿using System.Threading.Tasks;

using Abp.Dapper.Repositories;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Dapper.Tests.Domain;
using Abp.Events.Bus;
using Abp.Events.Bus.Entities;

using Shouldly;

using Xunit;

namespace Abp.EntityFrameworkCore.Dapper.Tests.Tests
{
    public class DomainEvents_Tests : AbpEfCoreDapperTestApplicationBase
    {
        private readonly IDapperRepository<Post> _postDapperRepository;
        private readonly IDapperRepository<Blog> _blogDapperRepository;
        private readonly IRepository<Blog> _blogRepository;
        private readonly IEventBus _eventBus;

        public DomainEvents_Tests()
        {
            _blogRepository = Resolve<IRepository<Blog>>();
            _blogDapperRepository = Resolve<IDapperRepository<Blog>>();
            _postDapperRepository = Resolve<IDapperRepository<Post>>();
            _eventBus = Resolve<IEventBus>();
        }

        [Fact]
        public void Should_Trigger_Domain_Events_For_Aggregate_Root()
        {
            //Arrange

            var isTriggered = false;

            _eventBus.Register<BlogUrlChangedEventData>(data =>
            {
                data.OldUrl.ShouldBe("http://testblog1.myblogs.com");
                isTriggered = true;
            });

            //Act

            Blog blog1 = _blogRepository.Single(b => b.Name == "test-blog-1");
            blog1.ChangeUrl("http://testblog1-changed.myblogs.com");
            _blogRepository.Update(blog1);


            //Assert
            var a = _postDapperRepository.FirstOrDefault(p => p.Title != null);
            a.ShouldNotBeNull();
            _blogDapperRepository.Get(blog1.Id).ShouldNotBeNull();
            isTriggered.ShouldBeTrue();
        }

        [Fact]
        public async Task should_trigger_event_on_inserted_with_dapper()
        {
            var triggerCount = 0;

            Resolve<IEventBus>().Register<EntityCreatedEventData<Blog>>(
                eventData =>
                {
                    eventData.Entity.Name.ShouldBe("OnSoftware");
                    eventData.Entity.IsTransient().ShouldBe(false);
                    triggerCount++;
                });

            _blogDapperRepository.Insert(new Blog("OnSoftware", "www.aspnetboilerplate.com"));

            triggerCount.ShouldBe(1);
        }
    }
}
