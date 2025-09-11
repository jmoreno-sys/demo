using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.EntityFrameworkCore
{
    public static class ProviderTypeConventions
    {
        public static void AddProviderTypeConventions(this ModelBuilder modelBuilder, string provider = "", string[] exclude = null)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif

            if (exclude == null || exclude.Length == 0)
                exclude = new[] { "Identity" };

            // Fix datetime offset support for integration tests
            // See: https://blog.dangl.me/archive/handling-datetimeoffset-in-sqlite-with-entity-framework-core/
            if (provider == "Sqlite")
            {
                // SQLite does not have proper support for DateTimeOffset via Entity Framework Domain, see the limitations
                // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
                // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
                // use the DateTimeOffsetToBinaryConverter
                // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
                // This only supports millisecond precision, but should be sufficient for most use cases.
                var datetimeProperties = modelBuilder.Model.GetEntityTypes()
                  .Where(m => !m.IsOwned())
                  .SelectMany(m => m.GetProperties())
                  .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
                  .ToArray();
                foreach (var p in datetimeProperties)
                {
                    var property = modelBuilder.Entity(p.DeclaringEntityType.ClrType).Property(p.Name);
                    property.HasConversion(new DateTimeOffsetToBinaryConverter());
                }

                var guidProperties = modelBuilder.Model.GetEntityTypes()
                  .Where(m => !m.IsOwned())
                  .SelectMany(m => m.GetProperties())
                  .Where(m => m.ClrType == typeof(Guid) || m.ClrType == typeof(Guid?))
                  .ToArray();

                foreach (var p in guidProperties)
                {
                    var property = modelBuilder.Entity(p.DeclaringEntityType.ClrType).Property(p.Name);
                    //property.ValueGeneratedNever();

                    if (p.IsColumnNullable())
                    {
                        // var converter = new ValueConverter<Guid?, string>(
                        //   v => v.ToString().ToUpperInvariant(),
                        //   v => !string.IsNullOrEmpty(v) ? new Guid(v) : default);
                        //property.HasConversion(converter);
                        property.HasDefaultValue();
                    }
                    else
                    {
                        // var converter = new ValueConverter<Guid, string>(
                        //   v => v.ToString().ToUpperInvariant(),
                        //   v => !string.IsNullOrEmpty(v) ? new Guid(v) : Guid.NewGuid());
                        //property.HasConversion(converter);
                        property.HasDefaultValue(Guid.NewGuid());
                    }
                }
            }

            // Generic Fields
            var items1 = modelBuilder.Model.GetEntityTypes().Where(m => !exclude.Contains(m.Name)).SelectMany(t => t.GetProperties()).ToArray();
            foreach (var p in items1)
            {
                var entity = modelBuilder.Entity(p.DeclaringEntityType.ClrType).Property(p.Name);
                var columnType = p.GetColumnType();
                if (columnType != null) continue;
                if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                {
                    var presicion = 2;
                    if (p.Name == "Price" || p.Name == "Amount")
                        presicion = 6;
                    p.SetColumnType($"decimal(18,{presicion})");
                    columnType = p.GetColumnType();
                    entity.HasColumnType(columnType);
                }
                else if (p.ClrType == typeof(float) || p.ClrType == typeof(float?))
                {
                    p.SetColumnType("float");
                    columnType = p.GetColumnType();
                    entity.HasColumnType(columnType);
                }
                else if (p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
                {
                    p.SetColumnType("datetime");
                    columnType = p.GetColumnType();
                    entity.HasColumnType(columnType);
                }
                else if (p.ClrType == typeof(string))
                {
                    var maxValue = p.GetMaxLength();
                    var max = maxValue.HasValue ? maxValue.ToString() : "max";

                    p.SetColumnType(provider != "Sqlite" ? $"varchar({max})" : $"varchar(500)");
                    columnType = p.GetColumnType();
                    entity.HasColumnType(columnType);
                }
            }

#if DEBUG
            sw.Stop();
            Debug.WriteLine($"[INFO] - Elapsed time for provider type conventions: {sw.Elapsed}");
#endif
        }
    }
}
