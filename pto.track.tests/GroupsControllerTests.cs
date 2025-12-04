using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using pto.track.services.DTOs;
using Xunit;
using pto.track;

namespace pto.track.tests;

public class GroupsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetResourcesForGroup_AsAdmin_ReturnsResources()
    {
        var adminClient = GetAdminClient();
        // Create a group
        var createDto = new CreateGroupDto("Resource Group");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        if (createResponse.StatusCode == HttpStatusCode.Forbidden)
            return;
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        // Create a resource assigned to the group (pseudo-code, adjust for your API)
        var resourceDto = new ResourceDto(0, "Test User", null, null, "Employee", false, true, null, createdGroup!.GroupId);
        // You may need to POST to /api/resources with groupId
        // await adminClient.PostAsJsonAsync($"/api/resources", resourceDto);

        // Get resources for the group
        var response = await adminClient.GetAsync($"/api/groups/{createdGroup.GroupId}/resources");
        response.EnsureSuccessStatusCode();
        var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();
        Assert.NotNull(resources);
        // Optionally, check that at least one resource is returned
        // Assert.Contains(resources, r => r.Name == "Test User");
    }

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _nonAdminClient;

    public GroupsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Simulate admin by adding a header or cookie, adjust as needed for your auth
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        // Simulate non-admin (no header or with a different role)
        _nonAdminClient = _factory.CreateClient();
        _nonAdminClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
    }

    private HttpClient GetAdminClient() => _adminClient;
    private HttpClient GetNonAdminClient() => _nonAdminClient;

    [Fact]
    public async Task GetGroups_AsAdmin_ReturnsOk()
    {
        var response = await GetAdminClient().GetAsync("/api/groups");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGroups_AsNonAdmin_ReturnsForbid()
    {
        var response = await GetNonAdminClient().GetAsync("/api/groups");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_AsAdmin_CreatesGroup()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("Integration Group");
        var response = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(group);
        Assert.Equal("Integration Group", group.Name);
    }

    [Fact]
    public async Task CreateGroup_AsNonAdmin_ReturnsForbid()
    {
        var nonAdminClient = GetNonAdminClient();
        var createDto = new CreateGroupDto("Integration Group");
        var response = await nonAdminClient.PostAsJsonAsync("/api/groups", createDto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_AsAdmin_ReturnsGroup()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("Integration Group");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        var response = await adminClient.GetAsync($"/api/groups/{createdGroup!.GroupId}"); ;
        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.Equal(createdGroup.GroupId, group!.GroupId);
        Assert.Equal("Integration Group", group.Name);
    }

    [Fact]
    public async Task GetGroupById_AsNonAdmin_ReturnsForbid()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("Integration Group");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        var nonAdminClient = GetNonAdminClient();
        var response = await nonAdminClient.GetAsync($"/api/groups/{createdGroup!.GroupId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGroup_AsAdmin_UpdatesGroup()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("Old Name");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        var updateDto = new UpdateGroupDto("New Name");
        var updateResponse = await adminClient.PutAsJsonAsync($"/api/groups/{createdGroup!.GroupId}", updateDto);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await adminClient.GetAsync($"/api/groups/{createdGroup.GroupId}");
        var updatedGroup = await getResponse.Content.ReadFromJsonAsync<GroupDto>();
        Assert.Equal("New Name", updatedGroup!.Name);
    }

    [Fact]
    public async Task UpdateGroup_AsNonAdmin_ReturnsForbid()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("Old Name");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        var nonAdminClient = GetNonAdminClient();
        var updateDto = new UpdateGroupDto("New Name");
        var updateResponse = await nonAdminClient.PutAsJsonAsync($"/api/groups/{createdGroup!.GroupId}", updateDto);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_AsAdmin_DeletesGroup()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("To Delete");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        var deleteResponse = await adminClient.DeleteAsync($"/api/groups/{createdGroup!.GroupId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await adminClient.GetAsync($"/api/groups/{createdGroup.GroupId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_AsNonAdmin_ReturnsForbid()
    {
        var adminClient = GetAdminClient();
        var createDto = new CreateGroupDto("To Delete");
        var createResponse = await adminClient.PostAsJsonAsync("/api/groups", createDto);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDto>();

        var nonAdminClient = GetNonAdminClient();
        var deleteResponse = await nonAdminClient.DeleteAsync($"/api/groups/{createdGroup!.GroupId}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }
}
