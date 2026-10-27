using Microsoft.EntityFrameworkCore;

namespace SklepInternetowyDocker.Data;

public static class DatabaseSecurity
{
    public static async Task VerifyRuntimeRoleAsync(ApplicationDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls
                    OR has_schema_privilege(current_user, 'public', 'CREATE')
                    OR EXISTS (SELECT 1 FROM pg_class WHERE relowner = pg_roles.oid
                               AND relnamespace = 'public'::regnamespace)
                FROM pg_roles WHERE rolname = current_user
                """;
            if (await command.ExecuteScalarAsync() is not false)
            {
                throw new InvalidOperationException("Production database role must not have administrative privileges, schema CREATE, or table ownership. Use a separate migrator.");
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}