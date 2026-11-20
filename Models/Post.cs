using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StrayThoughts_BE.Utilities;

namespace StrayThoughts_BE.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(30)]
    [Column(TypeName = "nvarchar(max)")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [Column(TypeName = "nvarchar(max)")]
    public string Body { get; set; } = string.Empty;

    [StringLength(32)]
    [Column(TypeName = "nvarchar(32)")]
    public string? BadgeEmoji { get; set; }

    [Required]
    public Guid ThemeId { get; set; }

    [ForeignKey(nameof(ThemeId))]
    public Theme Theme { get; set; } = null!;

    public DateTime? AdminHideDatetime { get; set; }

    public DateTime CreatedAt { get; set; } = ProjectClock.Now;
}
