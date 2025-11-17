using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    public class SchedulerResource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}
