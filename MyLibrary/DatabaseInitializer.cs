using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public class DatabaseInitializer : CreateDatabaseIfNotExists<DatabaseContext>
    {
        protected override void Seed(DatabaseContext context)
        {
            context.Messages.Add(new Message() { Text = "Hello" });
            context.Messages.Add(new Message() { Text = "Good day" });
            context.Messages.Add(new Message() { Text = "Everything is awesome" });
        }
    }
}
