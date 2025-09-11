using Dominio.Interfaces;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.EntityFrameworkCore
{
    internal static class AuditableEntitiesConventions
    {
        public static void AddAuditableEntitiesConventions(this ModelBuilder modelBuilder, string provider, string[] exclude = null)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            if (exclude == null || exclude.Length == 0)
                exclude = System.Array.Empty<string>();

            // IAuditable Interface

            var defaultDateFunction = provider switch
            {
                "MySql" => "now()",
                "Sqlite" => "CURRENT_TIMESTAMP",
                _ => "getdate()"
            };

            var items = modelBuilder.Model.GetEntityTypes().Where(m => !exclude.Contains(m.Name));
            foreach (var t in items)
            {
                var type = t.ClrType;
                if (!typeof(IAuditable).IsAssignableFrom(type)) continue;
                var properties = t.GetProperties().ToArray();
                if (!properties.Any()) continue;

                var created = properties.First(m => m.Name == nameof(IAuditable.FechaCreacion));
                created.SetDefaultValueSql(defaultDateFunction);
                t.AddIndex(created);

                var fechaMdificacion = properties.First(m => m.Name == nameof(IAuditable.FechaModificacion));
                t.AddIndex(fechaMdificacion);
            }

#if DEBUG
            sw.Stop();
            Debug.WriteLine($"[INFO] - Elapsed time for auditable entities conventions: {sw.Elapsed}");
#endif
        }
    }
}
