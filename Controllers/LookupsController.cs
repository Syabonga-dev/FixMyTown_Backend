using FixMyTownApi.Data;
using FixMyTownApi.Models.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyTownApi.Controllers
{
    /// <summary>
    /// Read-only reference data used to populate dropdowns and the
    /// category-picker grid in Step 2 of the report wizard. Public -
    /// no login needed.
    ///
    /// NOTE: the frontend expects PascalCase keys like "CategoryID"/
    /// "Name"/"DepartmentID" (inherited from the very first Node
    /// backend). GRP-03-39DB actually calls these CategoryId/
    /// DisplayName/DepartmentId, so this controller translates the
    /// shape here rather than touching the frontend.
    /// </summary>
    [Route("api/lookups")]
    [ApiController]
    [AllowAnonymous]
    public class LookupsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LookupsController(AppDbContext db) => _db = db;

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _db.Categories
                .OrderBy(c => c.DisplayName)
                .Select(c => new
                {
                    CategoryID = c.CategoryId,
                    Name = c.DisplayName,
                    c.Icon,
                    DepartmentID = c.DepartmentId
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("departments")]
        public async Task<ActionResult<IEnumerable<DepartmentReadDto>>> GetDepartments()
        {
            var departments = await _db.Departments
                .OrderBy(d => d.DisplayName)
                .Select(d => new DepartmentReadDto
                {
                    DepartmentID = d.DepartmentId,
                    Name = d.DisplayName,
                    OpenIssues = _db.Reports.Count(r =>
                        r.Category.DepartmentId == d.DepartmentId &&
                        r.Status != "Resolved" && r.DeletedAt == null),
                    WorkerCount = d.WorkerDepartments.Count(wd => wd.IsPrimary)
                })
                .ToListAsync();

            return Ok(departments);
        }
    }
}
