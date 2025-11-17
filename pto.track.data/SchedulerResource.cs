using System.ComponentModel.DataAnnotations;

namespace pto.track.data
{
    public class SchedulerResource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}
