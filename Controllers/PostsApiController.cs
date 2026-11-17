using StrayThoughts_BE.Data;
using StrayThoughts_BE.Models;
using StrayThoughts_BE.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text;

namespace StrayThoughts_BE.Controllers;

[Authorize(Roles = "Admin,Public")]
[ApiController]
[Route("api/posts")]
public class PostsApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("datatable")]
    public async Task<IActionResult> DataTable([FromQuery] string? visibility = null)
    {
        var request = DataTableRequest.BindFromQuery(HttpContext.Request.Query);
        //var isAdmin = User.IsInRole("Admin");
        var query = db.Posts.AsNoTracking();
        var q = Request.Query;
        query = /*!isAdmin ||*/ !string.Equals(visibility, "hidden", StringComparison.OrdinalIgnoreCase)
            ? query.Where(post => post.AdminHideDatetime == null)
            : query.Where(post => post.AdminHideDatetime != null);

        var recordsTotal = await query.CountAsync();
        var search = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(post => post.Title.Contains(search) || post.Body.Contains(search));
        }

        var titleSearch = request.Columns?.ElementAtOrDefault(0)?.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(titleSearch))
        {
            query = query.Where(post => post.Title.Contains(titleSearch));
        }

        var createdSearch = request.Columns?.ElementAtOrDefault(1)?.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(createdSearch))
        {
            query = query.Where(post => post.CreatedAt.ToString().Contains(createdSearch));
        }

        var recordsFiltered = await query.CountAsync();
        query = ApplyOrdering(query, request.Order?.FirstOrDefault()?.Column, request.Order?.FirstOrDefault()?.Dir);

        var data = await query
            .Skip(Math.Max(request.Start, 0))
            .Take(request.Length is > 0 and <= 100 ? request.Length : 10)
            .Select(post => new
            {
                post.Id,
                post.Title,
                post.CreatedAt,
                post.AdminHideDatetime
            })
            .ToListAsync();

        return Ok(new { draw = request.Draw, recordsTotal, recordsFiltered, data });
    }

    [AllowAnonymous]
    [HttpGet("public/feed")]
    public async Task<IActionResult> PublicFeed(
        [FromQuery] string? cursor = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] int pageSize = 20)
    {
        if (!TryDecodeCursor(cursor, out var cursorCreatedAt, out var cursorId))
        {
            return BadRequest(new { error = "The cursor is invalid." });
        }

        DateTime? fromDate = null;
        DateTime? toDateExclusive = null;
        if (!string.IsNullOrWhiteSpace(from))
        {
            if (!DateOnly.TryParse(from, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromLocalDate))
            {
                return BadRequest(new { error = "The from date must be a valid date." });
            }

            fromDate = fromLocalDate.ToDateTime(TimeOnly.MinValue);
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            if (!DateOnly.TryParse(to, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toLocalDate))
            {
                return BadRequest(new { error = "The to date must be a valid date." });
            }

            toDateExclusive = toLocalDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
        }

        if (fromDate.HasValue && toDateExclusive.HasValue && fromDate >= toDateExclusive)
        {
            return BadRequest(new { error = "The from date must be before or equal to the to date." });
        }

        pageSize = pageSize is > 0 and <= 100 ? pageSize : 20;

        var query = db.Posts
            .AsNoTracking()
            .Where(post => post.AdminHideDatetime == null);

        if (fromDate.HasValue)
        {
            query = query.Where(post => post.CreatedAt >= fromDate.Value);
        }

        if (toDateExclusive.HasValue)
        {
            query = query.Where(post => post.CreatedAt < toDateExclusive.Value);
        }

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            query = query.Where(post => post.CreatedAt < cursorCreatedAt ||
                post.CreatedAt == cursorCreatedAt && post.Id.CompareTo(cursorId) < 0);
        }

        var posts = await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Take(pageSize + 1)
            .Select(post => new
            {
                post.Id,
                post.Title,
                post.Body,
                post.BadgeEmoji,
                post.Theme,
                post.CreatedAt,
                post.AdminHideDatetime
            })
            .ToListAsync();

        var hasMore = posts.Count > pageSize;
        var data = posts.Take(pageSize).ToList();
        var nextCursor = hasMore
            ? EncodeCursor(data[^1].CreatedAt, data[^1].Id)
            : null;

        return Ok(new
        {
            data,
            hasMore,
            nextCursor
        });
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var value = $"{createdAt.Ticks}:{id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeCursor(string? cursor, out DateTime createdAt, out Guid id)
    {
        createdAt = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = value.Split(':', 2);
            return parts.Length == 2 &&
                long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) &&
                Guid.TryParse(parts[1], out id) &&
                ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks &&
                (createdAt = new DateTime(ticks, DateTimeKind.Unspecified)) != default;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var query = db.Posts.AsNoTracking().Where(p => p.Id == id);
        var post = await query.FirstOrDefaultAsync();
        if (post is null) return NotFound();
        return Ok(new
        {
            post.Id,
            post.Title,
            post.Body,
            post.BadgeEmoji,
            post.Theme,
            post.CreatedAt,
            post.AdminHideDatetime
        });
    }
    [AllowAnonymous]
    [HttpGet("public/{id:guid}")]
    public async Task<IActionResult> PublicDetails(Guid id)
    {
        var query = db.Posts.AsNoTracking().Where(p => p.Id == id).Where(p => p.AdminHideDatetime == null);
        var post = await query.FirstOrDefaultAsync();
        if (post is null) return NotFound();
        return Ok(new
        {
            post.Id,
            post.Title,
            post.Body,
            post.BadgeEmoji,
            post.Theme,
            post.CreatedAt
        });
        
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/hide")]
    public async Task<IActionResult> Hide(Guid id)
    {
        var post = await db.Posts.FirstOrDefaultAsync(item => item.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        post.AdminHideDatetime = ProjectClock.Now;
        db.PostAuditLogs.Add(CreateAuditLog(post.Id, "Hide", "Post hidden by admin."));
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var post = await db.Posts.FirstOrDefaultAsync(item => item.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        post.AdminHideDatetime = null;
        db.PostAuditLogs.Add(CreateAuditLog(post.Id, "Restore", "Post restored by admin."));
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static IQueryable<Post> ApplyOrdering(IQueryable<Post> query, int? column, string? direction)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (column, descending) switch
        {
            (0, false) => query.OrderBy(post => post.Title),
            (0, true) => query.OrderByDescending(post => post.Title),
            (1, false) => query.OrderBy(post => post.CreatedAt),
            (1, true) => query.OrderByDescending(post => post.CreatedAt),
            _ => query.OrderByDescending(post => post.CreatedAt)
        };
    }
    
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!EmojiValidator.IsValid(dto.BadgeEmoji))
        {
            ModelState.AddModelError(nameof(dto.BadgeEmoji),
                $"BadgeEmoji must be empty or contain only 1 emoji");
            return BadRequest(ModelState);
        }

        // Sanitize inputs by HTML encoding plain text fields
        var sanitizedTitle = WebUtility.HtmlEncode(dto.Title.Trim());
        var sanitizedBody = WebUtility.HtmlEncode(dto.Body.Trim());
        var theme = !string.IsNullOrWhiteSpace(dto.Theme) && PostThemes.Allowed.Contains(dto.Theme)
            ? dto.Theme
            : PostThemes.Default;

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = sanitizedTitle,
            Body = sanitizedBody,
            BadgeEmoji = dto.BadgeEmoji,
            Theme = theme,
            CreatedAt = ProjectClock.Now,
            AdminHideDatetime = null
        };

        db.Posts.Add(post);
        db.PostAuditLogs.Add(CreateAuditLog(post.Id, "Create", "Post created."));
        await db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Details),
            new { id = post.Id },
            new
            {
                post.Id,
                post.Title,
                post.Body,
                post.BadgeEmoji,
                post.Theme,
                post.CreatedAt,
                post.AdminHideDatetime
            }
        );
    }

    private PostAuditLog CreateAuditLog(Guid postId, string action, string details)
    {
        return new PostAuditLog
        {
            PostId = postId,
            Action = action,
            PerformedBy = User.Identity?.Name ?? "Anonymous", // to check User.Identity, di ngani guamgamit ng identity
            Timestamp = ProjectClock.Now,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
    }
}

public sealed class CreatePostDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(30, ErrorMessage = "Title cannot exceed 30 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Body is required.")]
    [StringLength(1000, ErrorMessage = "Body cannot exceed 1000 characters.")]
    public string Body { get; set; } = string.Empty;

    public string? BadgeEmoji { get; set; }

    public string Theme { get; set; } = string.Empty;
}
public sealed class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; } = 10;
    public DataTableSearch? Search { get; set; }
    public List<DataTableOrder>? Order { get; set; }
    public List<DataTableColumn>? Columns { get; set; }// Helper method to extract sorting even when model binding fails on double brackets
    public static DataTableRequest BindFromQuery(IQueryCollection query)
    {
        var request = new DataTableRequest
        {
            Draw = int.TryParse(query["draw"], out var d) ? d : 0,
            Start = int.TryParse(query["start"], out var s) ? s : 0,
            Length = int.TryParse(query["length"], out var l) ? l : 10,
            Search = new DataTableSearch { Value = query["search[value]"] }
        };

        var orders = new List<DataTableOrder>();
        int index = 0;
        while (query.ContainsKey($"order[{index}][column]"))
        {
            if (int.TryParse(query[$"order[{index}][column]"], out var colIndex))
            {
                orders.Add(new DataTableOrder
                {
                    Column = colIndex,
                    Dir = query[$"order[{index}][dir]"]
                });
            }
            index++;
        }

        if (orders.Count > 0)
        {
            request.Order = orders;
        }

        return request;
    }
}

public sealed class DataTableColumn
{
    public DataTableSearch? Search { get; set; }
}

public sealed class DataTableSearch
{
    public string? Value { get; set; }
}

public sealed class DataTableOrder
{
    public int Column { get; set; }
    public string? Dir { get; set; }
}
