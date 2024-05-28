using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.Data.Contexts
{
    public class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
    {
        public DbSet<Picture> PicturesDb { get; init; }
        public DbSet<Video> VideoDb { get; init; }
        public DbSet<CommentThread> CommentThreadsDb { get; init; }
        public DbSet<Comment> CommentsDb { get; init; }
        
    }
}
