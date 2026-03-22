using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL
{
    public class MyFeaturesDbContext : DbContext
    {
        public MyFeaturesDbContext(DbContextOptions<MyFeaturesDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TaskOccurrence> TaskOccurrences { get; set; }
        public DbSet<Notepad> Notepads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskTemplate>()
                .ToTable("TaskTemplates");

            modelBuilder.Entity<TaskOccurrence>()
                .ToTable("TaskOccurrences")
                .HasOne(it => it.TaskTemplate)
                .WithMany(i => i.TaskOccurrences)
                .HasForeignKey(it => it.TaskTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskOccurrence>()
                .HasIndex(i => i.DueDate)
                .HasDatabaseName("IDX_DueDate");

            modelBuilder.Entity<TaskOccurrence>()
                .HasIndex(ci => ci.TaskTemplateId)
                .HasDatabaseName("IDX_TaskTemplateID");

            modelBuilder.Entity<TaskOccurrence>()
                .HasIndex(ci => ci.CommittedDate)
                .HasDatabaseName("IDX_CommittedDate");

            modelBuilder.Entity<TaskOccurrence>()
                .HasIndex(ci => ci.CompletionDate)
                .HasDatabaseName("IDX_CompletionDate")
                .HasFilter("CompletionDate IS NOT NULL");
        }
    }
}
