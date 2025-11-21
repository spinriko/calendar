using System.ComponentModel.DataAnnotations;

namespace pto.track.data
{
    /// <summary>
    /// Represents a resource (employee or team member) in the scheduling system.
    /// </summary>
    public class SchedulerResource
    {
        /// <summary>
        /// Gets or sets the unique identifier for the resource.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the resource.
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the email address of the resource.
        /// </summary>
        [StringLength(255)]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the employee number for the resource.
        /// </summary>
        [StringLength(50)]
        public string? EmployeeNumber { get; set; }

        /// <summary>
        /// Gets or sets the role of the resource (e.g., Admin, Manager, Employee).
        /// </summary>
        [StringLength(50)]
        public string Role { get; set; } = "Employee";

        /// <summary>
        /// Gets or sets a value indicating whether this resource can approve absence requests.
        /// </summary>
        public bool IsApprover { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this resource is active in the system.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the department the resource belongs to.
        /// </summary>
        [StringLength(100)]
        public string? Department { get; set; }

        /// <summary>
        /// Gets or sets the ID of the resource's manager.
        /// </summary>
        public int? ManagerId { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the resource's manager.
        /// </summary>
        public SchedulerResource? Manager { get; set; }

        /// <summary>
        /// Gets or sets the Active Directory identifier for the resource.
        /// </summary>
        [StringLength(255)]
        public string? ActiveDirectoryId { get; set; }

        /// <summary>
        /// Gets or sets the date when the resource was last synchronized with an external system.
        /// </summary>
        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the resource record was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the resource record was last modified.
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }
}
