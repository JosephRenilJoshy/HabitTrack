using HabitService.Models;
using System.ComponentModel.DataAnnotations;

namespace HabitService.DTOs
{
    public class CreateHabitDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public FrequencyType FrequencyType { get; set; }

        [Range(1,7)]
        public int? TargetPerWeek { get; set; }
    }
}
