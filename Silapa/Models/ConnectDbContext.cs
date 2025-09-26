using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Silapa.Models;
public class ConnectDbContext : IdentityDbContext
    {
        public ConnectDbContext(DbContextOptions<ConnectDbContext> options)
            : base(options) { }

        //public DbSet<LoginViewModel> LoginViewModel { get; set; }
       public DbSet<AspNetUsers> AspNetUsers { get; set; }
       public DbSet<category> category { get;}
       public DbSet<Competitionlist> Competitionlist { get;}

    }