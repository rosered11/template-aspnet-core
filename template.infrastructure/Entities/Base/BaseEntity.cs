using Sieve.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace template.infrastructure.Entities
{
    public abstract class BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Sieve(CanFilter = true)]
        public string Channel { get; set; }
        [Sieve(CanFilter = true, CanSort = true, Name = "created")]
        public DateTime? CreatedAt { get; set; }
        [Sieve(CanFilter = true, CanSort = true, Name = "updated")]
        public DateTime? UpdatedAt { get; set; }
    }
}
