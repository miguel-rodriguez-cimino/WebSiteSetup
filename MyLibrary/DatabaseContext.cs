using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
            : base("DatabaseConnection")
        {
        }

        public DbSet<Message> Messages { get; set; }
    }
}
