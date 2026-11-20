using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrayThoughts_BE.Data;
using StrayThoughts_BE.Models;
using StrayThoughts_BE.Utilities;

namespace StrayThoughts_BE.Controllers;

[Authorize(Roles = "Admin,Public")]
[ApiController]
[Route("api/themes")]
public class ThemesController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    [Route("~/Themes")]
    public IActionResult Index() => View();

    [HttpGet("datatable")]
    public async Task<IActionResult> DataTable([FromQuery] string status = "active")
    {
        var request = DataTableRequest.BindFromQuery(HttpContext.Request.Query);
        var query = db.Themes.AsNoTracking();

        query = status.ToLowerInvariant() switch
        {
            "deleted" => query.Where(theme => theme.DeletedAt != null),
            "all" => query,
            _ => query.Where(theme => theme.DeletedAt == null)
        };

        var recordsTotal = await query.CountAsync();
        var search = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(theme => theme.Key.Contains(search) || theme.DisplayName.Contains(search));
        }

        var recordsFiltered = await query.CountAsync();
        var order = request.Order?.FirstOrDefault();
        query = ApplyOrdering(query, order?.Column, order?.Dir);

        var data = await query
            .Skip(Math.Max(request.Start, 0))
            .Take(request.Length is > 0 and <= 100 ? request.Length : 10)
            .Select(theme => new
            {
                theme.Id,
                theme.Key,
                theme.DisplayName,
                theme.DeletedAt,
                theme.CreatedAt,
                theme.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { draw = request.Draw, recordsTotal, recordsFiltered, data });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var theme = await db.Themes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return theme is null ? NotFound() : Ok(theme);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ThemeDto dto)
    {
        if (!TryNormalizeAndValidate(dto, out var normalized, out var validationErrors))
        {
            AddValidationErrors(validationErrors);
            return BadRequest(ModelState);
        }

        if (await AddDuplicateErrorsAsync(normalized.Key, normalized.DisplayName, null))
        {
            return BadRequest(ModelState);
        }

        var theme = new Theme
        {
            Key = normalized.Key,
            DisplayName = normalized.DisplayName,
            LightBg = normalized.LightBg,
            LightBorder = normalized.LightBorder,
            LightText = normalized.LightText,
            DarkBg = normalized.DarkBg,
            DarkBorder = normalized.DarkBorder,
            DarkText = normalized.DarkText,
            CreatedAt = ProjectClock.Now,
            UpdatedAt = ProjectClock.Now
        };

        db.Themes.Add(theme);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Details), new { id = theme.Id }, theme);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ThemeDto dto)
    {
        var theme = await db.Themes.FirstOrDefaultAsync(item => item.Id == id);
        if (theme is null)
        {
            return NotFound();
        }

        if (!TryNormalizeAndValidate(dto, out var normalized, out var validationErrors))
        {
            AddValidationErrors(validationErrors);
            return BadRequest(ModelState);
        }

        if (await AddDuplicateErrorsAsync(normalized.Key, normalized.DisplayName, id))
        {
            return BadRequest(ModelState);
        }

        theme.Key = normalized.Key;
        theme.DisplayName = normalized.DisplayName;
        theme.LightBg = normalized.LightBg;
        theme.LightBorder = normalized.LightBorder;
        theme.LightText = normalized.LightText;
        theme.DarkBg = normalized.DarkBg;
        theme.DarkBorder = normalized.DarkBorder;
        theme.DarkText = normalized.DarkText;
        theme.UpdatedAt = ProjectClock.Now;
        await db.SaveChangesAsync();
        return Ok(theme);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var theme = await db.Themes.FirstOrDefaultAsync(item => item.Id == id);
        if (theme is null)
        {
            return NotFound();
        }

        if (theme.DeletedAt is not null)
        {
            return BadRequest(new { error = "The theme is already deleted." });
        }

        theme.DeletedAt = ProjectClock.Now;
        theme.UpdatedAt = ProjectClock.Now;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var theme = await db.Themes.FirstOrDefaultAsync(item => item.Id == id);
        if (theme is null)
        {
            return NotFound();
        }

        if (theme.DeletedAt is null)
        {
            return BadRequest(new { error = "The theme is already active." });
        }

        theme.DeletedAt = null;
        theme.UpdatedAt = ProjectClock.Now;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> AddDuplicateErrorsAsync(string key, string displayName, Guid? excludedId)
    {
        var duplicateKey = await db.Themes.AsNoTracking()
            .Where(theme => (!excludedId.HasValue || theme.Id != excludedId.Value) && theme.Key.ToLower() == key)
            .Select(theme => new { theme.DeletedAt })
            .FirstOrDefaultAsync();
        if (duplicateKey is not null)
        {
            ModelState.AddModelError(nameof(ThemeDto.Key), $"Key already exists in a {(duplicateKey.DeletedAt is null ? "active" : "inactive")} theme.");
        }

        var duplicateDisplayName = await db.Themes.AsNoTracking()
            .Where(theme => (!excludedId.HasValue || theme.Id != excludedId.Value) && theme.DisplayName.ToLower() == displayName)
            .Select(theme => new { theme.DeletedAt })
            .FirstOrDefaultAsync();
        if (duplicateDisplayName is not null)
        {
            ModelState.AddModelError(nameof(ThemeDto.DisplayName), $"Display name already exists in a {(duplicateDisplayName.DeletedAt is null ? "active" : "inactive")} theme.");
        }

        return !ModelState.IsValid;
    }

    private static bool TryNormalizeAndValidate(ThemeDto dto, out NormalizedTheme normalized, out Dictionary<string, string> errors)
    {
        errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var key = dto.Key?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = dto.DisplayName?.Trim().ToLowerInvariant() ?? string.Empty;
        var colors = new Dictionary<string, string?>
        {
            [nameof(ThemeDto.LightBg)] = dto.LightBg,
            [nameof(ThemeDto.LightBorder)] = dto.LightBorder,
            [nameof(ThemeDto.LightText)] = dto.LightText,
            [nameof(ThemeDto.DarkBg)] = dto.DarkBg,
            [nameof(ThemeDto.DarkBorder)] = dto.DarkBorder,
            [nameof(ThemeDto.DarkText)] = dto.DarkText
        };

        if (string.IsNullOrWhiteSpace(key)) errors[nameof(ThemeDto.Key)] = "Key is required.";
        if (key.Length > 100) errors[nameof(ThemeDto.Key)] = "Key cannot exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(displayName)) errors[nameof(ThemeDto.DisplayName)] = "Display name is required.";
        if (displayName.Length > 100) errors[nameof(ThemeDto.DisplayName)] = "Display name cannot exceed 100 characters.";

        foreach (var color in colors)
        {
            var value = color.Value?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!Regex.IsMatch(value, "^#[0-9a-f]{6}$"))
            {
                errors[color.Key] = "Color must be a six-digit hexadecimal value.";
            }
        }

        normalized = new NormalizedTheme(
            key,
            displayName,
            NormalizeColor(dto.LightBg),
            NormalizeColor(dto.LightBorder),
            NormalizeColor(dto.LightText),
            NormalizeColor(dto.DarkBg),
            NormalizeColor(dto.DarkBorder),
            NormalizeColor(dto.DarkText));
        return errors.Count == 0;
    }

    private static string NormalizeColor(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private void AddValidationErrors(Dictionary<string, string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }

    private static IQueryable<Theme> ApplyOrdering(IQueryable<Theme> query, int? column, string? direction)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (column, descending) switch
        {
            (0, false) => query.OrderBy(theme => theme.Key),
            (0, true) => query.OrderByDescending(theme => theme.Key),
            (1, false) => query.OrderBy(theme => theme.DisplayName),
            (1, true) => query.OrderByDescending(theme => theme.DisplayName),
            (2, false) => query.OrderBy(theme => theme.DeletedAt),
            (2, true) => query.OrderByDescending(theme => theme.DeletedAt),
            _ => query.OrderBy(theme => theme.Key)
        };
    }

    private sealed record NormalizedTheme(
        string Key,
        string DisplayName,
        string LightBg,
        string LightBorder,
        string LightText,
        string DarkBg,
        string DarkBorder,
        string DarkText);
}

public sealed class ThemeDto
{
    [Required]
    [MaxLength(100)]
    public string? Key { get; set; }

    [Required]
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [Required]
    public string? LightBg { get; set; }

    [Required]
    public string? LightBorder { get; set; }

    [Required]
    public string? LightText { get; set; }

    [Required]
    public string? DarkBg { get; set; }

    [Required]
    public string? DarkBorder { get; set; }

    [Required]
    public string? DarkText { get; set; }
}
