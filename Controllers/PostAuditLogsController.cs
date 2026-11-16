using StrayThoughts_BE.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StrayThoughts_BE.Controllers;

[Authorize(Roles = "Admin,Public")]
[ApiController]
[Route("api/post-audit-logs")]
public class PostAuditLogsController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    [Route("~/PostAuditLogs")]
    public IActionResult Index() => View();

    [HttpGet("datatable")]
    public async Task<IActionResult> DataTable([FromQuery] DataTableRequest request)
    {
        var query = db.PostAuditLogs
            .AsNoTracking()
            .Join(db.Posts.AsNoTracking(), log => log.PostId, post => post.Id, (log, post) => new AuditLogRow
            {
                Log = log,
                Post = post
            });

        var recordsTotal = await query.CountAsync();
        var search = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(row => row.Log.Action.Contains(search) ||
                                       row.Log.PerformedBy.Contains(search) ||
                                       row.Log.Details!.Contains(search) ||
                                       row.Post.Title.Contains(search));
        }

        var columnSearches = request.Columns ?? [];
        var postIdSearch = columnSearches.ElementAtOrDefault(0)?.Search?.Value?.Trim();
        var titleSearch = columnSearches.ElementAtOrDefault(1)?.Search?.Value?.Trim();
        var actionSearch = columnSearches.ElementAtOrDefault(2)?.Search?.Value?.Trim();
        var performedBySearch = columnSearches.ElementAtOrDefault(3)?.Search?.Value?.Trim();
        var timestampSearch = columnSearches.ElementAtOrDefault(4)?.Search?.Value?.Trim();

        if (!string.IsNullOrWhiteSpace(postIdSearch) && Guid.TryParse(postIdSearch, out var postId))
        {
            query = query.Where(row => row.Log.PostId == postId);
        }
        else if (!string.IsNullOrWhiteSpace(postIdSearch))
        {
            query = query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(titleSearch))
        {
            query = query.Where(row => row.Post.Title.Contains(titleSearch));
        }

        if (!string.IsNullOrWhiteSpace(actionSearch))
        {
            query = query.Where(row => row.Log.Action.Contains(actionSearch));
        }

        if (!string.IsNullOrWhiteSpace(performedBySearch))
        {
            query = query.Where(row => row.Log.PerformedBy.Contains(performedBySearch));
        }

        if (!string.IsNullOrWhiteSpace(timestampSearch))
        {
            query = query.Where(row => row.Log.Timestamp.ToString().Contains(timestampSearch));
        }

        var recordsFiltered = await query.CountAsync();
        var order = request.Order?.FirstOrDefault();
        query = ApplyOrdering(query, order?.Column, order?.Dir);

        var data = await query
            .Skip(Math.Max(request.Start, 0))
            .Take(request.Length is > 0 and <= 100 ? request.Length : 10)
            .Select(row => new
            {
                row.Log.Id,
                row.Log.PostId,
                PostTitle = row.Post.Title,
                row.Log.Action,
                row.Log.PerformedBy,
                row.Log.Timestamp
            })
            .ToListAsync();

        return Ok(new { draw = request.Draw, recordsTotal, recordsFiltered, data });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await db.PostAuditLogs
            .AsNoTracking()
            .Where(log => log.Id == id)
            .Join(db.Posts.AsNoTracking(), log => log.PostId, post => post.Id, (log, post) => new
            {
                AuditLog = new
                {
                    log.Id,
                    log.PostId,
                    log.Action,
                    log.PerformedBy,
                    log.Timestamp,
                    log.Details,
                    log.IpAddress
                },
                Post = new
                {
                    post.Id,
                    post.Title,
                    post.Body,
                    post.CreatedAt,
                    post.AdminHideDatetime,
                    post.BadgeEmoji,
                    post.Theme
                }
            })
            .FirstOrDefaultAsync();

        return result is null ? NotFound() : Ok(result);
    }

    private static IQueryable<AuditLogRow> ApplyOrdering(IQueryable<AuditLogRow> query, int? column, string? direction)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (column, descending) switch
        {
            (0, false) => query.OrderBy(row => row.Log.PostId),
            (0, true) => query.OrderByDescending(row => row.Log.PostId),
            (1, false) => query.OrderBy(row => row.Post.Title),
            (1, true) => query.OrderByDescending(row => row.Post.Title),
            (2, false) => query.OrderBy(row => row.Log.Action),
            (2, true) => query.OrderByDescending(row => row.Log.Action),
            (3, false) => query.OrderBy(row => row.Log.PerformedBy),
            (3, true) => query.OrderByDescending(row => row.Log.PerformedBy),
            (4, false) => query.OrderBy(row => row.Log.Timestamp),
            _ => query.OrderByDescending(row => row.Log.Timestamp)
        };
    }

    private sealed class AuditLogRow
    {
        public required Models.PostAuditLog Log { get; init; }
        public required Models.Post Post { get; init; }
    }
}
