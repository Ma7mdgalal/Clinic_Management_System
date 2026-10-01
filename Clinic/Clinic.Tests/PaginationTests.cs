using Clinic.Application.Services;
using Clinic.Tests.TestDoubles;
using Xunit;

namespace Clinic.Tests;

/// <summary>
/// CMS-014. Pagination is exercised through PatientService because all three services
/// share the same ToPagedResult shape, so a rule proven here holds for the others.
/// </summary>
public class PaginationTests
{
    private readonly FakePatientRepository _patients = new();
    private readonly PatientService _sut;

    public PaginationTests()
    {
        _sut = new PatientService(
            _patients, new RecordingLogger<PatientService>(), new FakeAuthService());
    }

    private void Seed(int count)
    {
        for (var i = 0; i < count; i++)
        {
            // Zero-padded so alphabetical and numeric order agree, which keeps the
            // assertions below readable.
            _patients.Rows.Add(Build.Patient($"Patient{i:D3}", "Test"));
        }
    }

    [Fact]
    public async Task FirstPage_ReturnsTheFirstPageSizeItems()
    {
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 1, pageSize: 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Patient000", result.Items[0].FirstName);
        Assert.Equal("Patient009", result.Items[9].FirstName);
    }

    [Fact]
    public async Task MiddlePage_SkipsTheCorrectNumberOfItems()
    {
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 2, pageSize: 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Patient010", result.Items[0].FirstName);
    }

    [Fact]
    public async Task LastPage_ReturnsTheRemainder()
    {
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 3, pageSize: 10);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal("Patient024", result.Items[4].FirstName);
    }

    [Fact]
    public async Task PageBeyondTheEnd_ReturnsEmptyItems_NotAnError()
    {
        // A client that walks off the end has asked a valid question with an empty answer.
        // 200 with zero items is the right reply; 404 would imply the collection is missing.
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 99, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task TotalCount_CountsEveryMatch_NotJustTheCurrentPage()
    {
        // The whole point of TotalCount: the client needs to know how many rows exist so it
        // can render a page control, not how many it happened to receive.
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 1, pageSize: 10);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    [InlineData(100, 10, 10)]
    public async Task TotalPages_RoundsUp(int rows, int pageSize, int expectedPages)
    {
        // Integer division would report 2 pages for 25 rows and silently hide the last five.
        // Math.Ceiling on a double is what makes the partial page count.
        Seed(rows);

        var result = await _sut.GetAllAsync(pageNumber: 1, pageSize: pageSize);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(int.MinValue)]
    public async Task NonPositivePageNumber_IsCoercedToOne(int pageNumber)
    {
        // Current behaviour, pinned so a refactor cannot turn it into a negative Skip —
        // which throws ArgumentOutOfRangeException and surfaces as a 500.
        //
        // Sprint 3 review 3.4: silently correcting a client bug also hides it. Prefer
        // [Range] on the query parameter and a 400. When you make that change, this test
        // should change with it.
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: pageNumber, pageSize: 10);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.Items.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositivePageSize_FallsBackToTen(int pageSize)
    {
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 1, pageSize: pageSize);

        Assert.Equal(10, result.PageSize);
        Assert.Equal(10, result.Items.Count);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.4.
    ///
    /// pageSize is unbounded. Verified live: ?pageSize=99999 returned 200 and echoed
    /// "pageSize": 99999. Combined with in-memory pagination (review 3.3, where the whole
    /// table is loaded before Skip/Take runs), one request materialises, maps and serialises
    /// every row — a denial-of-service lever handed to any authenticated caller.
    ///
    /// Fix with Math.Clamp(pageSize, 1, 100). Clamping is right here because the client
    /// asked for something merely excessive rather than invalid, and the response reports
    /// the size actually used.
    /// </summary>
    [Theory]
    [InlineData(101)]
    [InlineData(1000)]
    [InlineData(99999)]
    [InlineData(int.MaxValue)]
    public async Task ExcessivePageSize_IsCapped(int pageSize)
    {
        Seed(25);

        var result = await _sut.GetAllAsync(pageNumber: 1, pageSize: pageSize);

        Assert.True(
            result.PageSize <= 100,
            $"pageSize={pageSize} was accepted unchanged; an unbounded page size lets one " +
            "request load the entire table.");
    }

    [Fact]
    public async Task EmptyRepository_ReturnsAWellFormedEmptyPage()
    {
        // A client rendering a page control must not have to special-case "no data".
        var result = await _sut.GetAllAsync(pageNumber: 1, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task SearchAndPaginationCompose_CountIsOfMatchesNotOfAllRows()
    {
        // TotalCount must reflect the filtered set. Counting before the filter tells the
        // client there are more pages than exist, and the extra pages come back empty.
        _patients.Rows.Add(Build.Patient("Match", "One"));
        _patients.Rows.Add(Build.Patient("Match", "Two"));
        _patients.Rows.Add(Build.Patient("Other", "Three"));

        var result = await _sut.GetAllAsync(search: "match", pageNumber: 1, pageSize: 1);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
    }

    /// <summary>
    /// Sprint 3 review 3.3 — the ordering problem, stated as a test.
    ///
    /// No service orders its query, and SQL Server guarantees no ordering without ORDER BY.
    /// Skip/Take over an unordered set can return the same row on two different pages and
    /// omit another entirely. The in-memory fake happens to preserve insertion order, so
    /// this test passes today and would keep passing after the query moves to the database —
    /// which is exactly why it is worth having: it will start failing if pagination moves to
    /// SQL without an OrderBy.
    /// </summary>
    [Fact]
    public async Task PagesDoNotOverlapOrSkipRows()
    {
        Seed(25);

        var seen = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await _sut.GetAllAsync(pageNumber: page, pageSize: 10);
            seen.AddRange(result.Items.Select(i => i.FirstName));
        }

        Assert.Equal(25, seen.Count);
        Assert.Equal(25, seen.Distinct().Count());
    }
}
