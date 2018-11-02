using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaMediaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaMediaApi.Data
{
    public class MediaDbContext : DbContext
    {
        public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
        {

        }

        public DbSet<Picture> PicturesDb { get; set; }
        public DbSet<Video> VideoDb { get; set; }
        public DbSet<CommentThread> CommentThreadsDb { get; set; }
        public DbSet<Comment> CommentsDb { get; set; }

    }
}
