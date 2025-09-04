using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Project.Models
{
    public class SchedulerEvent
    {
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Text { get; set; }
        public string? Color { get; set; }

        [JsonPropertyName("resource")]
        public int ResourceId { get; set; }
    }
}
