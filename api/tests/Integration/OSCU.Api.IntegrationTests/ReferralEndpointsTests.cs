using System.Net;
using System.Net.Http.Json;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Api.IntegrationTests;

/// <summary>
/// End-to-end coverage of the referral endpoints: HTTP in, real Postgres out.
/// </summary>
[TestFixture]
public class ReferralEndpointsTests
{
    private ReferralApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new ReferralApiFactory();
        await _factory.InitialiseAsync();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [SetUp]
    public Task SetUp() => _factory.ResetAsync();

    private static CreateReferralRequest ACreateRequest(string reference = "REF-0001") =>
        new(reference,
            "Safeguarding concern",
            "Details from the referring officer.",
            ReferralStatus.New,
            new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc));

    private async Task<ReferralResponse> GivenAReferral(string reference = "REF-0001")
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/referrals", ACreateRequest(reference));
        response.EnsureSuccessStatusCode();

        ApiEnvelope<ReferralResponse>? body = await response.Content.ReadFromJsonAsync<ApiEnvelope<ReferralResponse>>();

        return body!.Data!;
    }

    [Test]
    public async Task Post_GivenAValidRequest_ShouldReturn201WithALocationHeader()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/referrals", ACreateRequest());

        ApiEnvelope<ReferralResponse>? body = await response.Content.ReadFromJsonAsync<ApiEnvelope<ReferralResponse>>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location, Is.Not.Null);
            Assert.That(body!.Data!.ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(body.Data.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public async Task Post_GivenADuplicateReference_ShouldReturn409()
    {
        await GivenAReferral("REF-0001");

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/referrals", ACreateRequest("REF-0001"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Post_GivenAnInvalidRequest_ShouldReturn400WithFieldLevelErrors()
    {
        CreateReferralRequest invalid = new(
            string.Empty, string.Empty, null, "Marinated", default);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/referrals", invalid);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(body, Does.Contain(nameof(CreateReferralRequest.ReferralReference)));
            Assert.That(body, Does.Contain(nameof(CreateReferralRequest.Subject)));
            Assert.That(body, Does.Contain(nameof(CreateReferralRequest.Status)));
        });
    }

    [Test]
    public async Task Post_ShouldPersistReceivedDateAsUtc()
    {
        // Proves the timestamptz round-trip: what goes in comes back out as
        // the same instant with Kind Utc, rather than drifting by the server's
        // timezone offset.
        ReferralResponse created = await GivenAReferral();

        ApiEnvelope<ReferralResponse>? fetched = await _client
            .GetFromJsonAsync<ApiEnvelope<ReferralResponse>>($"/api/referrals/{created.Id}");

        Assert.Multiple(() =>
        {
            Assert.That(fetched!.Data!.ReceivedDate, Is.EqualTo(new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc)));
            Assert.That(fetched.Data.ReceivedDate.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [Test]
    public async Task Get_GivenAnUnknownId_ShouldReturn404()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/referrals/{Guid.CreateVersion7()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetList_ShouldPageAndReportTheUnpagedTotal()
    {
        for (int i = 1; i <= 12; i++)
        {
            await GivenAReferral($"REF-{i:D4}");
        }

        ApiEnvelope<PagedResult<ReferralResponse>>? body = await _client
            .GetFromJsonAsync<ApiEnvelope<PagedResult<ReferralResponse>>>("/api/referrals?page=2&pageSize=5");

        Assert.Multiple(() =>
        {
            Assert.That(body!.Data!.Items, Has.Count.EqualTo(5));
            Assert.That(body.Data.TotalCount, Is.EqualTo(12));
            Assert.That(body.Data.TotalPages, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task GetList_GivenAPageSizeAboveTheCap_ShouldReturn400()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/referrals?pageSize={ReferralListQuery.MaxPageSize + 1}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetList_GivenASearchTerm_ShouldFilterOnReferenceAndSubject()
    {
        await GivenAReferral("REF-0001");
        await GivenAReferral("REF-0002");

        ApiEnvelope<PagedResult<ReferralResponse>>? body = await _client
            .GetFromJsonAsync<ApiEnvelope<PagedResult<ReferralResponse>>>("/api/referrals?search=REF-0002");

        Assert.Multiple(() =>
        {
            Assert.That(body!.Data!.TotalCount, Is.EqualTo(1));
            Assert.That(body.Data.Items[0].ReferralReference, Is.EqualTo("REF-0002"));
        });
    }

    [Test]
    public async Task Put_GivenAnExistingReferral_ShouldApplyTheAmendment()
    {
        ReferralResponse created = await GivenAReferral();
        UpdateReferralRequest request = new(
            "Amended subject",
            "Amended description",
            ReferralStatus.InProgress,
            new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc));

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/referrals/{created.Id}", request);
        ApiEnvelope<ReferralResponse>? body = await response.Content.ReadFromJsonAsync<ApiEnvelope<ReferralResponse>>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body!.Data!.Subject, Is.EqualTo("Amended subject"));
            Assert.That(body.Data.Status, Is.EqualTo(ReferralStatus.InProgress));
            // The business reference is not amendable.
            Assert.That(body.Data.ReferralReference, Is.EqualTo("REF-0001"));
        });
    }

    [Test]
    public async Task Put_GivenAnUnknownId_ShouldReturn404()
    {
        UpdateReferralRequest request = new(
            "Amended subject", null, ReferralStatus.Closed,
            new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc));

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/referrals/{Guid.CreateVersion7()}", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_GivenAnExistingReferral_ShouldReturn204AndRemoveIt()
    {
        ReferralResponse created = await GivenAReferral();

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/api/referrals/{created.Id}");
        HttpResponseMessage getResponse = await _client.GetAsync($"/api/referrals/{created.Id}");

        Assert.Multiple(() =>
        {
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task Delete_GivenAnUnknownId_ShouldReturn404()
    {
        HttpResponseMessage response = await _client.DeleteAsync($"/api/referrals/{Guid.CreateVersion7()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetStatuses_ShouldReturnTheControlledVocabulary()
    {
        ApiEnvelope<IReadOnlyCollection<string>>? body = await _client
            .GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<string>>>("/api/referrals/statuses");

        Assert.That(body!.Data, Is.EquivalentTo(ReferralStatus.All));
    }

    [Test]
    public async Task Health_ShouldReportHealthyWhenTheDatabaseIsReachable()
    {
        HttpResponseMessage response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
