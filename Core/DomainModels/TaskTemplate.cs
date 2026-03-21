using Core.Enum;

namespace Core.DomainModels
{
    public class TaskTemplate
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool Recurring { get; set; }
        public bool? RenewOnDueDate { get; set; }
        public int? IntervalValue { get; set; }
        public IntervalType? IntervalType { get; set; }
        public int? RowIndex { get; set; }
    }
}