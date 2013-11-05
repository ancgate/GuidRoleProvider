using GuidRoleProvider.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace GuidRoleProvider
{
    public class RoleProviderContext : DbContext
    {
        public RoleProviderContext() : base("RoleProviderContext")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}
