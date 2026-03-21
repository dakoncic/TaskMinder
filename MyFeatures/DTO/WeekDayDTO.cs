namespace MyFeatures.DTO
{
    public class WeekDayDto
    {
        public DateTime WeekDayDate { get; set; }
        public List<TaskOccurrenceDto> TaskOccurrences { get; set; }
    }
}
