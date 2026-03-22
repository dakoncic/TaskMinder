using Shared.Enum;
using Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class TaskTemplate : BaseEntity, IHasRowIndex
    {
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool Recurring { get; set; }
        public bool? RenewOnDueDate { get; set; }
        public int? IntervalValue { get; set; }
        public IntervalType? IntervalType { get; set; }

        public int? RowIndex { get; set; }

        public bool Completed { get; set; }
        public ICollection<TaskOccurrence> TaskOccurrences { get; set; } = new List<TaskOccurrence>();
    }
}
