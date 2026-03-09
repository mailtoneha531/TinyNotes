using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    public class Note
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? Summary { get; set; }
        public string? Hashtags { get; set; }
    }

    public class NotesDbContext : DbContext
    {
        public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options) { }
        public DbSet<Note> Notes { get; set; }
    }
}
