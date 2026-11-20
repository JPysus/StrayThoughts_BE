using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StrayThoughts_BE.Utilities;

namespace StrayThoughts_BE.Models;

public class Theme
{
    public const string DefaultKey = "sunny-joy";

    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(7)]
    [Column(TypeName = "varchar(7)")]
    public string LightBg { get; set; } = "#fffbeb";

    [Required]
    [MaxLength(7)]
    [Column(TypeName = "varchar(7)")]
    public string LightBorder { get; set; } = "#fde68a";

    [Required]
    [MaxLength(7)]
    [Column(TypeName = "varchar(7)")]
    public string LightText { get; set; } = "#78350f";

    [Required]
    [MaxLength(7)]
    [Column(TypeName = "varchar(7)")]
    public string DarkBg { get; set; } = "#0f172a";

    [Required]
    [MaxLength(7)]
    [Column(TypeName = "varchar(7)")]
    public string DarkBorder { get; set; } = "#1e293b";

    [Required]
    [MaxLength(7)]
    [Column(TypeName = "varchar(7)")]
    public string DarkText { get; set; } = "#fef3c7";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = ProjectClock.Now;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Post> Posts { get; set; } = [];
}
