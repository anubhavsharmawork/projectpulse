using System.Reflection;

namespace Infrastructure.Sql
{
    public static class SqlLoader
    {
        private static readonly Assembly _assembly = typeof(SqlLoader).Assembly;

        public static string Load(string name)
        {
            var resourceName = $"Infrastructure.Sql.{name}";
            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded SQL resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string EnsureBaseSchema => Load("EnsureBaseSchema.sql");
        public static string DropSchema => Load("DropSchema.sql");
        public static string CreateSchema => Load("CreateSchema.sql");
        public static string GrantSchema => Load("GrantSchema.sql");
        public static string WipeData => Load("WipeData.sql");
    }
}
