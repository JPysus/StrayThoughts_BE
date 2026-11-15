using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Theme { get; set; } = PostThemes.Default;

    public DateTime? AdminHideDatetime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
