﻿using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abp.Dapper.Tests.Entities
{
    [Table("Person")]
    public class Person : Entity, IMustHaveTenant
    {
        protected Person()
        {
        }

        public Person(string name) : this()
        {
            Name = name;
        }

        public virtual string Name { get; set; }

        public Guid TenantId { get; set; }
    }
}
