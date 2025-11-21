using System.Text.Json;
using pto.track.services.DTOs;

namespace pto.track.services.tests;

public class DtoSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public DtoSerializationTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public void EventDto_SerializesToCamelCase()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var dto = new EventDto(
            Id: testId,
            Start: new DateTime(2025, 1, 15, 9, 0, 0),
            End: new DateTime(2025, 1, 15, 17, 0, 0),
            Text: "Test Event",
            Color: "#2e78d6cc",
            ResourceId: 5
        );

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("id", out var idProp));
        Assert.Equal(testId, Guid.Parse(idProp.GetString()!));

        Assert.True(root.TryGetProperty("start", out var startProp));
        Assert.Equal(dto.Start, startProp.GetDateTime());

        Assert.True(root.TryGetProperty("end", out var endProp));
        Assert.Equal(dto.End, endProp.GetDateTime());

        Assert.True(root.TryGetProperty("text", out var textProp));
        Assert.Equal("Test Event", textProp.GetString());

        Assert.True(root.TryGetProperty("color", out var colorProp));
        Assert.Equal("#2e78d6cc", colorProp.GetString());

        Assert.True(root.TryGetProperty("resource", out var resourceProp));
        Assert.Equal(5, resourceProp.GetInt32());
    }

    [Fact]
    public void EventDto_DoesNotSerializeWithPascalCase()
    {
        // Arrange
        var dto = new EventDto(
            Id: Guid.NewGuid(),
            Start: DateTime.Now,
            End: DateTime.Now.AddHours(1),
            Text: "Test",
            Color: null,
            ResourceId: 1
        );

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Assert - Should NOT have PascalCase properties
        Assert.False(root.TryGetProperty("Id", out _));
        Assert.False(root.TryGetProperty("Start", out _));
        Assert.False(root.TryGetProperty("End", out _));
        Assert.False(root.TryGetProperty("Text", out _));
        Assert.False(root.TryGetProperty("ResourceId", out _));
    }

    [Fact]
    public void CreateEventDto_DeserializesFromCamelCase()
    {
        // Arrange
        var json = """
            {
                "start": "2025-01-15T09:00:00",
                "end": "2025-01-15T17:00:00",
                "text": "New Event",
                "color": "#6aa84fcc",
                "resource": 3
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<CreateEventDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(new DateTime(2025, 1, 15, 9, 0, 0), dto.Start);
        Assert.Equal(new DateTime(2025, 1, 15, 17, 0, 0), dto.End);
        Assert.Equal("New Event", dto.Text);
        Assert.Equal("#6aa84fcc", dto.Color);
        Assert.Equal(3, dto.ResourceId);
    }

    [Fact]
    public void UpdateEventDto_DeserializesFromCamelCase()
    {
        // Arrange
        var json = """
            {
                "start": "2025-02-20T10:30:00",
                "end": "2025-02-20T14:00:00",
                "text": "Updated Event",
                "color": "#f1c232cc",
                "resource": 7
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<UpdateEventDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(new DateTime(2025, 2, 20, 10, 30, 0), dto.Start);
        Assert.Equal(new DateTime(2025, 2, 20, 14, 0, 0), dto.End);
        Assert.Equal("Updated Event", dto.Text);
        Assert.Equal("#f1c232cc", dto.Color);
        Assert.Equal(7, dto.ResourceId);
    }

    [Fact]
    public void ResourceDto_SerializesToCamelCase()
    {
        // Arrange
        var dto = new ResourceDto(
            Id: 10,
            Name: "Conference Room A",
            Email: "room-a@test.com",
            EmployeeNumber: "EMP-001",
            Role: "Employee",
            IsApprover: false,
            IsActive: true,
            Department: "Facilities");

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("id", out var idProp));
        Assert.Equal(10, idProp.GetInt32());

        Assert.True(root.TryGetProperty("name", out var nameProp));
        Assert.Equal("Conference Room A", nameProp.GetString());
    }

    [Fact]
    public void ResourceDto_DoesNotSerializeWithPascalCase()
    {
        // Arrange
        var dto = new ResourceDto(
            Id: 1,
            Name: "Test Resource",
            Email: null,
            EmployeeNumber: null,
            Role: "Employee",
            IsApprover: false,
            IsActive: true,
            Department: null);

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Assert - Should NOT have PascalCase properties
        Assert.False(root.TryGetProperty("Id", out _));
        Assert.False(root.TryGetProperty("Name", out _));
    }

    [Fact]
    public void EventDto_RoundTripSerialization()
    {
        // Arrange
        var originalDto = new EventDto(
            Id: Guid.NewGuid(),
            Start: new DateTime(2025, 3, 10, 8, 0, 0),
            End: new DateTime(2025, 3, 10, 16, 30, 0),
            Text: "Round Trip Test",
            Color: "#cc4125cc",
            ResourceId: 12
        );

        // Act
        var json = JsonSerializer.Serialize(originalDto, _jsonOptions);
        var deserializedDto = JsonSerializer.Deserialize<EventDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserializedDto);
        Assert.Equal(originalDto.Id, deserializedDto.Id);
        Assert.Equal(originalDto.Start, deserializedDto.Start);
        Assert.Equal(originalDto.End, deserializedDto.End);
        Assert.Equal(originalDto.Text, deserializedDto.Text);
        Assert.Equal(originalDto.Color, deserializedDto.Color);
        Assert.Equal(originalDto.ResourceId, deserializedDto.ResourceId);
    }

    [Fact]
    public void EventDto_SerializedJsonMatchesExpectedFormat()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var dto = new EventDto(
            Id: testId,
            Start: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            End: new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Text: "Format Test",
            Color: null,
            ResourceId: 1
        );

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);

        // Assert
        Assert.Contains($"\"id\":\"{testId}\"", json);
        Assert.Contains("\"start\":", json);
        Assert.Contains("\"end\":", json);
        Assert.Contains("\"text\":\"Format Test\"", json);
        Assert.Contains("\"resource\":1", json);

        // Should NOT contain PascalCase
        Assert.DoesNotContain("\"Id\":", json);
        Assert.DoesNotContain("\"Start\":", json);
        Assert.DoesNotContain("\"ResourceId\":", json);
    }
}
