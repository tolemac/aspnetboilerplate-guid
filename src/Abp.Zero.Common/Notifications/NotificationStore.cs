﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Expressions;
using Abp.Linq.Extensions;

namespace Abp.Notifications
{
    /// <summary>
    /// Implements <see cref="INotificationStore"/> using repositories.
    /// </summary>
    public class NotificationStore : INotificationStore, ITransientDependency
    {
        private readonly IRepository<NotificationInfo> _notificationRepository;
        private readonly IRepository<TenantNotificationInfo> _tenantNotificationRepository;
        private readonly IRepository<UserNotificationInfo> _userNotificationRepository;
        private readonly IRepository<NotificationSubscriptionInfo> _notificationSubscriptionRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationStore"/> class.
        /// </summary>
        public NotificationStore(
            IRepository<NotificationInfo> notificationRepository,
            IRepository<TenantNotificationInfo> tenantNotificationRepository,
            IRepository<UserNotificationInfo> userNotificationRepository,
            IRepository<NotificationSubscriptionInfo> notificationSubscriptionRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _notificationRepository = notificationRepository;
            _tenantNotificationRepository = tenantNotificationRepository;
            _userNotificationRepository = userNotificationRepository;
            _notificationSubscriptionRepository = notificationSubscriptionRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        [UnitOfWork]
        public virtual async Task InsertSubscriptionAsync(NotificationSubscriptionInfo subscription)
        {
            using (_unitOfWorkManager.Current.SetTenantId(subscription.TenantId))
            {
                await _notificationSubscriptionRepository.InsertAsync(subscription);
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual async Task DeleteSubscriptionAsync(UserIdentifier user, string notificationName, string entityTypeName, string entityId)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                await _notificationSubscriptionRepository.DeleteAsync(s =>
                    s.UserId == user.UserId &&
                    s.NotificationName == notificationName &&
                    s.EntityTypeName == entityTypeName &&
                    s.EntityId == entityId
                    );
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual async Task InsertNotificationAsync(NotificationInfo notification)
        {
            using (_unitOfWorkManager.Current.SetTenantId(null))
            {
                await _notificationRepository.InsertAsync(notification);
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual async Task<NotificationInfo> GetNotificationOrNullAsync(Guid notificationId)
        {
            using (_unitOfWorkManager.Current.SetTenantId(null))
            {
                return await _notificationRepository.FirstOrDefaultAsync(notificationId);
            }
        }

        [UnitOfWork]
        public virtual async Task InsertUserNotificationAsync(UserNotificationInfo userNotification)
        {
            using (_unitOfWorkManager.Current.SetTenantId(userNotification.TenantId))
            {
                await _userNotificationRepository.InsertAsync(userNotification);
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual Task<List<NotificationSubscriptionInfo>> GetSubscriptionsAsync(string notificationName, string entityTypeName, string entityId)
        {
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                return _notificationSubscriptionRepository.GetAllListAsync(s =>
                    s.NotificationName == notificationName &&
                    s.EntityTypeName == entityTypeName &&
                    s.EntityId == entityId
                    );
            }
        }

        [UnitOfWork]
        public virtual async Task<List<NotificationSubscriptionInfo>> GetSubscriptionsAsync(Guid?[] tenantIds, string notificationName, string entityTypeName, string entityId)
        {
            var subscriptions = new List<NotificationSubscriptionInfo>();

            foreach (var tenantId in tenantIds)
            {
                subscriptions.AddRange(await GetSubscriptionsAsync(tenantId, notificationName, entityTypeName, entityId));
            }

            return subscriptions;
        }

        [UnitOfWork]
        public virtual async Task<List<NotificationSubscriptionInfo>> GetSubscriptionsAsync(UserIdentifier user)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                return await _notificationSubscriptionRepository.GetAllListAsync(s => s.UserId == user.UserId);
            }
        }

        [UnitOfWork]
        protected virtual async Task<List<NotificationSubscriptionInfo>> GetSubscriptionsAsync(Guid? tenantId, string notificationName, string entityTypeName, string entityId)
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                return await _notificationSubscriptionRepository.GetAllListAsync(s =>
                    s.NotificationName == notificationName &&
                    s.EntityTypeName == entityTypeName &&
                    s.EntityId == entityId
                );
            }
        }

        [UnitOfWork]
        public virtual async Task<bool> IsSubscribedAsync(UserIdentifier user, string notificationName, string entityTypeName, string entityId)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                return await _notificationSubscriptionRepository.CountAsync(s =>
                    s.UserId == user.UserId &&
                    s.NotificationName == notificationName &&
                    s.EntityTypeName == entityTypeName &&
                    s.EntityId == entityId
                    ) > 0;
            }
        }

        [UnitOfWork]
        public virtual async Task UpdateUserNotificationStateAsync(Guid? tenantId, Guid userNotificationId, UserNotificationState state)
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var userNotification = await _userNotificationRepository.FirstOrDefaultAsync(userNotificationId);
                if (userNotification == null)
                {
                    return;
                }

                userNotification.State = state;
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual async Task UpdateAllUserNotificationStatesAsync(UserIdentifier user, UserNotificationState state)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                var userNotifications = await _userNotificationRepository.GetAllListAsync(un => un.UserId == user.UserId);

                foreach (var userNotification in userNotifications)
                {
                    userNotification.State = state;
                }

                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual async Task DeleteUserNotificationAsync(Guid? tenantId, Guid userNotificationId)
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                await _userNotificationRepository.DeleteAsync(userNotificationId);
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [UnitOfWork]
        public virtual async Task DeleteAllUserNotificationsAsync(UserIdentifier user, UserNotificationState? state = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                var predicate = CreateNotificationFilterPredicate(user, state, startDate, endDate);

                await _userNotificationRepository.DeleteAsync(predicate);
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        private Expression<Func<UserNotificationInfo, bool>> CreateNotificationFilterPredicate(UserIdentifier user, UserNotificationState? state = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var predicate = PredicateBuilder.True<UserNotificationInfo>();
            predicate = predicate.And(p => p.UserId == user.UserId);

            if (startDate.HasValue)
            {
                predicate = predicate.And(p => p.CreationTime >= startDate);
            }

            if (endDate.HasValue)
            {
                predicate = predicate.And(p => p.CreationTime <= endDate);
            }

            if (state.HasValue)
            {
                predicate = predicate.And(p => p.State == state);
            }

            return predicate;
        }

        [UnitOfWork]
        public virtual Task<List<UserNotificationInfoWithNotificationInfo>> GetUserNotificationsWithNotificationsAsync(UserIdentifier user, UserNotificationState? state = null, int skipCount = 0, int maxResultCount = int.MaxValue, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                var query = from userNotificationInfo in _userNotificationRepository.GetAll()
                            join tenantNotificationInfo in _tenantNotificationRepository.GetAll() on userNotificationInfo.TenantNotificationId equals tenantNotificationInfo.Id
                            where userNotificationInfo.UserId == user.UserId
                            orderby tenantNotificationInfo.CreationTime descending
                            select new { userNotificationInfo, tenantNotificationInfo = tenantNotificationInfo };

                if (state.HasValue)
                {
                    query = query.Where(x => x.userNotificationInfo.State == state.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(x => x.tenantNotificationInfo.CreationTime >= startDate);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(x => x.tenantNotificationInfo.CreationTime <= endDate);
                }

                query = query.PageBy(skipCount, maxResultCount);

                var list = query.ToList();

                return Task.FromResult(list.Select(
                    a => new UserNotificationInfoWithNotificationInfo(a.userNotificationInfo, a.tenantNotificationInfo)
                ).ToList());
            }
        }

        [UnitOfWork]
        public virtual async Task<int> GetUserNotificationCountAsync(UserIdentifier user, UserNotificationState? state = null)
        {
            using (_unitOfWorkManager.Current.SetTenantId(user.TenantId))
            {
                return await _userNotificationRepository.CountAsync(un => un.UserId == user.UserId && (state == null || un.State == state.Value));
            }
        }

        [UnitOfWork]
        public virtual Task<UserNotificationInfoWithNotificationInfo> GetUserNotificationWithNotificationOrNullAsync(Guid? tenantId, Guid userNotificationId)
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var query = from userNotificationInfo in _userNotificationRepository.GetAll()
                            join tenantNotificationInfo in _tenantNotificationRepository.GetAll() on userNotificationInfo.TenantNotificationId equals tenantNotificationInfo.Id
                            where userNotificationInfo.Id == userNotificationId
                            select new { userNotificationInfo, tenantNotificationInfo = tenantNotificationInfo };

                var item = query.FirstOrDefault();
                if (item == null)
                {
                    return Task.FromResult((UserNotificationInfoWithNotificationInfo)null);
                }

                return Task.FromResult(new UserNotificationInfoWithNotificationInfo(item.userNotificationInfo, item.tenantNotificationInfo));
            }
        }

        [UnitOfWork]
        public virtual async Task InsertTenantNotificationAsync(TenantNotificationInfo tenantNotificationInfo)
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantNotificationInfo.TenantId))
            {
                await _tenantNotificationRepository.InsertAsync(tenantNotificationInfo);
            }
        }

        public virtual Task DeleteNotificationAsync(NotificationInfo notification)
        {
            return _notificationRepository.DeleteAsync(notification);
        }
    }
}
