using System.ComponentModel.DataAnnotations;
using Xunit;

namespace pto.track.data.tests;

public class SchedulerEventValidationTests
{
    [Fact]
    public void Validate_EndAfterStart_NoValidationErrors()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = 1
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EndBeforeStart_ReturnsValidationError()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 10, 0, 0),
            End = new DateTime(2025, 1, 1, 9, 0, 0),
            ResourceId = 1
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Single(results);
        Assert.Contains("End must be greater than Start", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_EndEqualsStart_ReturnsValidationError()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 9, 0, 0),
            ResourceId = 1
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Single(results);
        Assert.Contains("End must be greater than Start", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_ResourceIdZero_ReturnsValidationError()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = 0
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Single(results);
        Assert.Contains("ResourceId must be a positive integer", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_ResourceIdNegative_ReturnsValidationError()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = -1
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Single(results);
        Assert.Contains("ResourceId must be a positive integer", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_TextExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = 1,
            Text = new string('A', 201)
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Single(results);
        Assert.Contains(results, r => r.ErrorMessage?.Contains("length") == true);
    }

    [Fact]
    public void Validate_ColorExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = 1,
            Color = new string('B', 51)
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Single(results);
        Assert.Contains(results, r => r.ErrorMessage?.Contains("length") == true);
    }

    [Fact]
    public void Validate_TextAtMaxLength_NoValidationErrors()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = 1,
            Text = new string('A', 200)
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_ColorAtMaxLength_NoValidationErrors()
    {
        // Arrange
        var schedulerEvent = new SchedulerEvent
        {
            Start = new DateTime(2025, 1, 1, 9, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0),
            ResourceId = 1,
            Color = new string('B', 50)
        };

        // Act
        var results = ValidateModel(schedulerEvent);

        // Assert
        Assert.Empty(results);
    }

    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}

public class AbsenceRequestValidationTests
{
    [Fact]
    public void Validate_EndAfterStart_NoValidationErrors()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EndBeforeStart_ReturnsValidationError()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(2),
            End = DateTime.UtcNow.AddDays(1),
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Single(results);
        Assert.Contains("End must be greater than Start", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_EndEqualsStart_ReturnsValidationError()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(1);
        var request = new AbsenceRequest
        {
            Start = startDate,
            End = startDate,
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Single(results);
        Assert.Contains("End must be greater than Start", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_PastDateWithPendingStatus_ReturnsValidationError()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(-2),
            End = DateTime.UtcNow.AddDays(-1),
            Reason = "Sick leave",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.ErrorMessage?.Contains("Cannot request absence for past dates") == true);
    }

    [Fact]
    public void Validate_PastDateWithApprovedStatus_NoValidationError()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(-2),
            End = DateTime.UtcNow.AddDays(-1),
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            ApproverId = 2,
            ApprovedDate = DateTime.UtcNow.AddDays(-3)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.DoesNotContain(results, r => r.ErrorMessage?.Contains("Cannot request absence for past dates") == true);
    }

    [Fact]
    public void Validate_ReasonExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = new string('A', 501),
            EmployeeId = 1,
            Status = AbsenceStatus.Pending
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.ErrorMessage?.Contains("length") == true);
    }

    [Fact]
    public void Validate_ReasonAtMaxLength_NoValidationErrors()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = new string('A', 500),
            EmployeeId = 1,
            Status = AbsenceStatus.Pending
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_ApprovalCommentsExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            ApproverId = 2,
            ApprovedDate = DateTime.UtcNow,
            ApprovalComments = new string('B', 1001)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.ErrorMessage?.Contains("length") == true);
    }

    [Fact]
    public void Validate_ApprovalCommentsAtMaxLength_NoValidationErrors()
    {
        // Arrange
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            ApproverId = 2,
            ApprovedDate = DateTime.UtcNow,
            ApprovalComments = new string('B', 1000)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void DefaultStatus_IsPending()
    {
        // Arrange & Act
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = "Test"
        };

        // Assert
        Assert.Equal(AbsenceStatus.Pending, request.Status);
    }

    [Fact]
    public void RequestedDate_DefaultsToUtcNow()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var request = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(1),
            End = DateTime.UtcNow.AddDays(2),
            Reason = "Test"
        };
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(request.RequestedDate, before, after);
    }

    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}

public class ResourceValidationTests
{
    [Fact]
    public void Validate_NameWithinMaxLength_NoValidationErrors()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "Resource A"
        };

        // Act
        var results = ValidateModel(resource);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_NameAtMaxLength_NoValidationErrors()
    {
        // Arrange
        var resource = new Resource
        {
            Name = new string('A', 100)
        };

        // Act
        var results = ValidateModel(resource);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var resource = new Resource
        {
            Name = new string('A', 101)
        };

        // Act
        var results = ValidateModel(resource);

        // Assert
        Assert.Single(results);
        Assert.Contains(results, r => r.ErrorMessage?.Contains("length") == true);
    }

    [Fact]
    public void Validate_NameRequired_ReturnsValidationError()
    {
        // Arrange
        var resource = new Resource
        {
            Name = null!
        };

        // Act
        var results = ValidateModel(resource);

        // Assert
        Assert.Contains(results, r => r.ErrorMessage?.Contains("required") == true || r.ErrorMessage?.Contains("Name") == true);
    }

    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}

public class GroupValidationTests
{
    [Fact]
    public void Validate_NameRequired_NoValidationErrors()
    {
        // Arrange
        var group = new pto.track.data.Models.Group
        {
            Name = "Test Group"
        };

        // Act
        var results = ValidateModel(group);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_NameRequired_ReturnsValidationError()
    {
        // Arrange
        var group = new pto.track.data.Models.Group
        {
            Name = null!
        };

        // Act
        var results = ValidateModel(group);

        // Assert
        Assert.Contains(results, r => r.ErrorMessage?.Contains("required") == true || r.ErrorMessage?.Contains("Name") == true);
    }

    [Fact]
    public void Validate_NameWithValue_NoValidationErrors()
    {
        // Arrange
        var group = new pto.track.data.Models.Group
        {
            GroupId = 1,
            Name = "Engineering Team"
        };

        // Act
        var results = ValidateModel(group);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_GroupIdCanBeSet()
    {
        // Arrange & Act
        var group = new pto.track.data.Models.Group
        {
            GroupId = 5,
            Name = "Test Group"
        };

        // Assert
        Assert.Equal(5, group.GroupId);
        Assert.Equal("Test Group", group.Name);
    }

    [Fact]
    public void Validate_ResourcesCollectionCanBeNull()
    {
        // Arrange
        var group = new pto.track.data.Models.Group
        {
            Name = "Test Group",
            Resources = null
        };

        // Act
        var results = ValidateModel(group);

        // Assert
        Assert.Empty(results);
        Assert.Null(group.Resources);
    }

    [Fact]
    public void Validate_ResourcesCollectionCanBeInitialized()
    {
        // Arrange & Act
        var group = new pto.track.data.Models.Group
        {
            Name = "Test Group",
            Resources = new List<Resource>
            {
                new Resource { Id = 1, Name = "Resource 1", GroupId = 1 },
                new Resource { Id = 2, Name = "Resource 2", GroupId = 1 }
            }
        };

        // Assert
        Assert.NotNull(group.Resources);
        Assert.Equal(2, group.Resources.Count);
        Assert.All(group.Resources, r => Assert.Equal(1, r.GroupId));
    }

    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
