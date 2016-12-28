using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaxFm.Core.Models;

namespace PlaxFm.Core.Store
{
    class PlaxContext: DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Setup> Setup { get; set; }
        public DbSet<Song> Songs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public PlaxContext(DbConnection connection) : base(connection, true) { }
    }
}
