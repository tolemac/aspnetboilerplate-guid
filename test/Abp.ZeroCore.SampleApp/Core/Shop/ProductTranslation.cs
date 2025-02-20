﻿using Abp.Domain.Entities;
using System;

namespace Abp.ZeroCore.SampleApp.Core.Shop
{
    public class ProductTranslation : Entity, IEntityTranslation<Product>
    {
        public virtual string Name { get; set; }

        public virtual Product Core { get; set; }

        public virtual Guid CoreId { get; set; }

        public virtual string Language { get; set; }
    }
}