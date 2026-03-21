using Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class Notepad : BaseEntity, IHasRowIndex
    {
        public string? Content { get; set; }

        [Required]
        public int? RowIndex { get; set; }
    }
}
