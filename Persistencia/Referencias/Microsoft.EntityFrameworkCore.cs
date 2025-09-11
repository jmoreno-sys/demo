using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public static class RelationalEntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> ToTable<TEntity>([NotNull] this EntityTypeBuilder<TEntity> entityTypeBuilder, string name, string schema, bool auto) where TEntity : class
        {
            if (!auto) return entityTypeBuilder.ToTable(name, schema);
            return string.IsNullOrEmpty(schema) ? entityTypeBuilder.ToTable(name) : entityTypeBuilder.ToTable(name, schema);
        }
    }

    public static class DbContextExtensions
    {
        public static void Rollback(this DbContext context)
        {
            var changedEntriesCopy = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        public static void Initialize(this DbContext context)
        {
            if (context.Database.ProviderName.EndsWith("Sqlite"))
                context.Database.EnsureCreated();
            else
                context.Database.Migrate();
        }
    }
}
