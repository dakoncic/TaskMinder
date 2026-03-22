using Core.Enum;

namespace Core.DomainModels
{
    public class TaskOccurrence
    {
        public int Id { get; set; }
        public int TaskTemplateId { get; set; }
        public DateTime? CommittedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? CompletionDate { get; set; }
        public required TaskTemplate TaskTemplate { get; set; }

        public TaskOccurrence CreateNewRecurringTask(DateTime currentLocalDateTime)
        {
            var newTaskOccurrence = new TaskOccurrence
            {
                TaskTemplateId = TaskTemplateId,
                TaskTemplate = TaskTemplate,
                Description = TaskTemplate.Description
            };

            if (DueDate is not null && TaskTemplate.IntervalValue is not null)
            {
                var daysBetween = CalculateDaysBetween(TaskTemplate, currentLocalDateTime);

                //ako je renewOnDueDate true, neće bit null jer postoji days between
                //npr. vit D svake ned.
                if (TaskTemplate.RenewOnDueDate!.Value)
                {
                    //na complete uvijek dodajem dane barem 1 put
                    newTaskOccurrence.DueDate = DueDate.Value.AddDays(daysBetween);

                    // i onda još dodaj dok ne bude dovoljno da taj datum bude veći od današnjeg dana (ako već nije)
                    while (newTaskOccurrence.DueDate.Value.Date <= currentLocalDateTime.Date)
                    {
                        newTaskOccurrence.DueDate = newTaskOccurrence.DueDate.Value.AddDays(daysBetween);
                    }
                }
                //inače se obnavlja na completion date npr. registracija auta
                else
                {
                    newTaskOccurrence.DueDate = currentLocalDateTime.AddDays(daysBetween);
                }

                //odma committamo
                newTaskOccurrence.CommittedDate = newTaskOccurrence.DueDate;
            }

            return newTaskOccurrence;
        }

        private static int CalculateDaysBetween(TaskTemplate taskTemplate, DateTime currentLocalDateTime)
        {
            if (taskTemplate.IntervalType!.Value == IntervalType.Months)
            {
                return CalculateDaysBetweenForMonths(taskTemplate.IntervalValue!.Value, currentLocalDateTime);
            }
            else
            {
                return taskTemplate.IntervalValue!.Value;
            }
        }

        private static int CalculateDaysBetweenForMonths(int months, DateTime currentLocalDateTime)
        {
            var startDate = currentLocalDateTime;
            var endDate = startDate.AddMonths(months);
            return (endDate - startDate).Days;
        }
    }

}
