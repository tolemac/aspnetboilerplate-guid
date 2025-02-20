using System;
using System.Linq;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;

namespace Abp.TestBase.SampleApplication.Messages
{
    public class MessageAppService : CrudAppService<Message, MessageDto, Guid, GetMessagesWithFilterInput>
    {
        public MessageAppService(IRepository<Message> repository)
            : base(repository)
        {

        }

        protected override IQueryable<Message> CreateFilteredQuery(GetMessagesWithFilterInput input)
        {
            return base.CreateFilteredQuery(input)
                .WhereIf(!input.Text.IsNullOrWhiteSpace(), m => m.Text.Contains(input.Text));
        }
    }
}