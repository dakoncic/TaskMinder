using Shared.Enum;
using Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class TaskTemplate : BaseEntity<int>, IHasRowIndex
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public bool Recurring { get; set; }

        //ako je false, onda će se days between dodat na completion date
        //za novi DueDate
        //ako je true onda na DueDate
        public bool? RenewOnDueDate { get; set; }
        public int? IntervalValue { get; set; }
        public IntervalType? IntervalType { get; set; }

        public int? RowIndex { get; set; }

        public bool Completed { get; set; }

        //nova lista se inicijalizira tako da nikad ne bude null
        public ICollection<TaskOccurrence> TaskOccurrences { get; set; } = new List<TaskOccurrence>();
    }
}
