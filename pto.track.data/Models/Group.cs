using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace pto.track.data.Models
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }

        [Required]
        public required string Name { get; set; }

        // Future extensible attributes
        // public string Description { get; set; }
        // public string Color { get; set; }

        public ICollection<Resource>? Resources { get; set; }
    }
}
