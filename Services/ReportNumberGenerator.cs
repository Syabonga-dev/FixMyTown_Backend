using FixMyTownApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FixMyTownApi.Services
{
    /// <summary>
    /// Builds a reference number like "GRP-260730-0001" - same format as
    /// the database's fn_GenerateReferenceNumber(), just done in C# since
    /// creating a Report through EF Core doesn't go through that function.
    /// </summary>
    public class ReportNumberGenerator
    {
        private readonly AppDbContext _db;

        public ReportNumberGenerator(AppDbContext db) => _db = db;

        public async Task<string> GenerateAsync()
        {
            var today = DateTime.UtcNow;
            var datePart = today.ToString("yyMMdd");

            var todaysCount = await _db.Reports.CountAsync(r => r.CreatedAt.Date == today.Date);
            var sequence = (todaysCount + 1).ToString("D4");

            return $"GRP-{datePart}-{sequence}";
        }
    }
}
