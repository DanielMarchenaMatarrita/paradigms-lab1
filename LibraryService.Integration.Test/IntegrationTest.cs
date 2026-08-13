using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using HackerRank1.Controllers;
using HackerRank1.DTO;
using LibraryService.WebAPI;
using LibraryService.WebAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Xunit;

namespace LibraryService.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> application;
    private readonly LibraryContext context;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        context = new LibraryContext(new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlite("DataSource=:memory:")
            .Options);
        context.Database.OpenConnection();

        application = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<LibraryContext>();
                services.AddSingleton(context);
            }));

        Client = application.CreateClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task Libraries_CanBeListedAndRetrieved()
    {
        var emptyResponse = await Client.GetAsync("/api/libraries");
        emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadBody<List<Library>>(emptyResponse)).Should().BeEmpty();

        var libraries = await SeedLibraries();

        var listResponse = await Client.GetAsync("/api/libraries");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadBody<List<Library>>(listResponse)).Should().HaveCount(2);

        var getResponse = await Client.GetAsync($"/api/libraries/{libraries[0].Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadBody<Library>(getResponse)).Name.Should().Be("Library 1");

        (await Client.GetAsync("/api/libraries/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LibraryCreation_UsesServerIdAndReturnsCreatedLocation()
    {
        var request = new Library { Id = 999, Name = "Created Library", Location = "Created Location" };

        var response = await PostJson("/api/libraries", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadBody<Library>(response);
        created.Id.Should().BePositive().And.NotBe(999);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/api/Libraries/{created.Id}");
        (await context.Libraries.AsNoTracking().SingleAsync()).Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task LibraryUpdate_UsesRouteIdAndHandlesMissingLibrary()
    {
        var libraries = await SeedLibraries();
        var routeLibrary = libraries[0];
        var bodyLibrary = libraries[1];

        var update = new Library
        {
            Id = bodyLibrary.Id,
            Name = "Updated by route",
            Location = "Updated location"
        };

        var response = await PutJson($"/api/libraries/{routeLibrary.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        context.ChangeTracker.Clear();
        var persistedRouteLibrary = await context.Libraries.AsNoTracking().SingleAsync(x => x.Id == routeLibrary.Id);
        var persistedBodyLibrary = await context.Libraries.AsNoTracking().SingleAsync(x => x.Id == bodyLibrary.Id);
        persistedRouteLibrary.Name.Should().Be("Updated by route");
        persistedBodyLibrary.Name.Should().Be("Library 2");

        (await PutJson("/api/libraries/99999", update)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BookCreation_UsesServerAndRouteIdsAndRequiresParent()
    {
        var libraries = await SeedLibraries();
        var request = new Book
        {
            Id = 999,
            Name = "Created Book",
            Category = "Testing",
            LibraryId = libraries[1].Id
        };

        var response = await PostJson($"/api/libraries/{libraries[0].Id}/books", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("\"library\":");
        var created = await ReadBody<Book>(response);
        created.Id.Should().BePositive().And.NotBe(999);
        created.LibraryId.Should().Be(libraries[0].Id);

        var missingResponse = await PostJson("/api/libraries/99999/books", request);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await context.Books.AsNoTracking().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Books_CanBeListedAndMissingParentReturnsNotFound()
    {
        var libraries = await SeedLibraries();
        await SeedBook("Book 1", libraries[0].Id);
        await SeedBook("Book 2", libraries[0].Id);
        await Authenticate();

        var populatedResponse = await Client.GetAsync($"/api/libraries/{libraries[0].Id}/books");
        populatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await ReadBody<List<Book>>(populatedResponse);
        books.Should().HaveCount(2).And.OnlyContain(book => book.LibraryId == libraries[0].Id);

        var emptyResponse = await Client.GetAsync($"/api/libraries/{libraries[1].Id}/books");
        emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadBody<List<Book>>(emptyResponse)).Should().BeEmpty();

        (await Client.GetAsync("/api/libraries/99999/books")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LibraryDeletion_CascadesBooksAndHandlesRepeatedDelete()
    {
        var libraries = await SeedLibraries();
        await SeedBook("Book to delete", libraries[0].Id);

        var response = await Client.DeleteAsync($"/api/libraries/{libraries[0].Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        context.ChangeTracker.Clear();
        (await context.Libraries.AsNoTracking().AnyAsync(x => x.Id == libraries[0].Id)).Should().BeFalse();
        (await context.Books.AsNoTracking().AnyAsync(x => x.LibraryId == libraries[0].Id)).Should().BeFalse();
        (await Client.DeleteAsync($"/api/libraries/{libraries[0].Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await Authenticate();
        (await Client.GetAsync($"/api/libraries/{libraries[0].Id}/books")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        Client.Dispose();
        application.Dispose();
        context.Dispose();
    }

    private async Task<List<Library>> SeedLibraries()
    {
        var libraries = new List<Library>
        {
            new() { Name = "Library 1", Location = "Location 1" },
            new() { Name = "Library 2", Location = "Location 2" }
        };

        await context.Libraries.AddRangeAsync(libraries);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return libraries;
    }

    private async Task SeedBook(string name, int libraryId)
    {
        await context.Books.AddAsync(new Book
        {
            Name = name,
            Category = "Testing",
            LibraryId = libraryId
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private async Task Authenticate()
    {
        var response = await PostJson("/login", new User
        {
            Email = "admin",
            Password = "1234",
            Role = "admin"
        });
        response.EnsureSuccessStatusCode();
        var token = await ReadBody<TokenResponse>(response);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.token);
    }

    private Task<HttpResponseMessage> PostJson<T>(string path, T value) =>
        Client.PostAsync(path, JsonContent(value));

    private Task<HttpResponseMessage> PutJson<T>(string path, T value) =>
        Client.PutAsync(path, JsonContent(value));

    private static StringContent JsonContent<T>(T value) =>
        new(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");

    private static async Task<T> ReadBody<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content)!;
    }
}
