using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Silapa.Controllers;
using Silapa.Models;
namespace Silapa.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Registerdetail>()
            .HasKey(rd => new { rd.no, rd.h_id, rd.Type }); // กำหนด composite key
            builder.Entity<dCompetitionlist>()
            .HasKey(rd => new { rd.id, rd.h_id });

            // 🚨 3. กำหนดความสัมพันธ์ (One-to-Many)
            builder.Entity<NewsImage>()
                .HasOne(ni => ni.News)
                .WithMany(n => n.GalleryImages) // GalleryImages คือ ICollection ใน Model news
                .HasForeignKey(ni => ni.NewsId)
                .OnDelete(DeleteBehavior.Cascade); // เมื่อลบข่าว รูปภาพที่เกี่ยวข้องจะถูกลบตามไปด้วย
        }

        internal string? Find(int id)
        {
            throw new NotImplementedException();
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = false;
        }
        //  public DbSet<ApplicationUser> ApplicationUser { get; set; }
        // public DbSet<AspNetUsers> AspNetUsers { get; set;}
        // Optionally, you can add other DbSets for other entities in your application.
        public DbSet<category> category { get; set; }
        public DbSet<Competitionlist> Competitionlist { get; set; }
        public DbSet<grouplist> grouplist { get; set; }
        public DbSet<school> school { get; set; }
        public DbSet<Affiliation> Affiliation { get; set; }
        public DbSet<Registerhead> Registerhead { get; set; }
        public DbSet<Registerdetail> Registerdetail { get; set; }
        public DbSet<Racelocation> Racelocation { get; set; }
        public DbSet<racedetails> racedetails { get; set; }
        public DbSet<referee> referee { get; set; }
        public DbSet<news> news { get; set; }
        public DbSet<setupsystem> setupsystem { get; set; }
        public DbSet<dCompetitionlist> dCompetitionlist { get; set; }
        public DbSet<contacts> contacts { get; set; }
        public DbSet<registerdirector> registerdirector { get; set; }
        public DbSet<VisitorCounts> VisitorCounts { get; set; }
        public DbSet<uploadfilepdf> uploadfilepdf { get; set; }
        public DbSet<groupreferee> groupreferee { get; set; }
        public DbSet<criterion> criterion { get; set; }
        public DbSet<deleteregister> deleteregister { get; set; }
        public DbSet<filelist> filelist { get; set; }
        public DbSet<Certificate> Certificate { get; set; }
        public DbSet<NewsImage> NewsImages { get; set; }
        public DbSet<TimelineItem>TimelineItem{ get; set; }
        public DbSet<AuditLog>AuditLog{get;set;}
    }
}