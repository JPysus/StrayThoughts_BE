using System.ComponentModel.DataAnnotations;

namespace StrayThoughts_BE.Models;

public class PostAuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PostId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // "Created", "Hidden", "Restored"

    [Required]
    [MaxLength(100)]
    public string PerformedBy { get; set; } = "Anonymous"; // Admin username or "Anonymous"

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; } // Optional descriptive text (e.g., "Post hidden by admin")

    public string? IpAddress { get; set; }
}
