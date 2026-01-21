using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace pto.track.data
{
    /// <summary>
    /// Represents a resource (employee or team member) in the system.
    /// </summary>
    public class Resource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? EmployeeNumber { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "Employee";

        public bool IsApprover { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? Department { get; set; }

        public int? ManagerId { get; set; }
        public Resource? Manager { get; set; }

        [StringLength(255)]
        public string? ActiveDirectoryId { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public int GroupId { get; set; }
        public Models.Group? Group { get; set; }

        // ADP Integration Fields (from SOURCE.ADP_EMPLOYEE view)
        
        /// <summary>
        /// ADP Associate ID - primary key from ADP system, matches AD employeeID
        /// </summary>
        [StringLength(50)]
        public string? AssociateId { get; set; }

        /// <summary>
        /// ADP Associate ID of this employee's manager (REPORTS_TO_ASSOCIATE_ID)
        /// </summary>
        [StringLength(50)]
        public string? ManagerAssociateId { get; set; }

        /// <summary>
        /// Department code from ADP (DEPARTMENT_CODE)
        /// </summary>
        [StringLength(50)]
        public string? DepartmentCode { get; set; }

        /// <summary>
        /// Job title from ADP (JOB_TITLE)
        /// </summary>
        [StringLength(100)]
        public string? JobTitle { get; set; }

        /// <summary>
        /// Job code from ADP (JOB_CODE)
        /// </summary>
        [StringLength(50)]
        public string? JobCode { get; set; }
    }
}
