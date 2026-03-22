using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.DataMigration;

/// <summary>
/// One-time migration tool: copies all data from Azure SQL Server to PostgreSQL.
/// Place this project at the solution root alongside the other projects.
///
/// Usage:
///   1. Update the connection strings below
///   2. Ensure PostgreSQL databases exist with schema applied (run EF migrations first)
///   3. Run: dotnet run --project KinaUna.DataMigration
/// </summary>
internal class Program
{
    // --- Source: SQL Server connection strings ---
    private const string SqlServerAuth = "";
    private const string SqlServerProgeny = "";
    private const string SqlServerMedia = "";

    // --- Target: PostgreSQL connection strings ---
    private const string PostgresAuth = "";
    private const string PostgresProgeny = "";
    private const string PostgresMedia = "";

    private static async Task Main()
    {
        Console.WriteLine("=== KinaUna Data Migration: SQL Server → PostgreSQL ===");
        Console.WriteLine();

        try
        {
            await MigrateAuthDatabase();
            await MigrateProgenyDatabase();
            await MigrateMediaDatabase();

            Console.WriteLine();
            Console.WriteLine("=== Migration complete! ===");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Verify row counts match between source and target");
            Console.WriteLine("  2. Set ResetOpenIddictDatabase=true and run the auth server to re-seed OpenIddict clients");
            Console.WriteLine("  3. Test login and basic functionality");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Migration failed: {ex.Message}");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }
    }

    // =========================================================================
    // Auth Database (Identity tables — OpenIddict will be re-seeded separately)
    // =========================================================================
    private static async Task MigrateAuthDatabase()
    {
        Console.WriteLine("--- Migrating Auth Database ---");

        await using var source = CreateContext<ApplicationDbContext>(SqlServerAuth, useSqlServer: true);
        await using var target = CreateContext<ApplicationDbContext>(PostgresAuth, useSqlServer: false);

        // Order matters: parent tables before child tables (FK dependencies)
        await CopyEntities<IdentityRole>(source, target, "AspNetRoles");
        await CopyEntities<ApplicationUser>(source, target, "AspNetUsers");
        await CopyEntities<IdentityRoleClaim<string>>(source, target, "AspNetRoleClaims");
        await CopyEntities<IdentityUserClaim<string>>(source, target, "AspNetUserClaims");
        await CopyEntities<IdentityUserLogin<string>>(source, target, "AspNetUserLogins");
        await CopyEntities<IdentityUserRole<string>>(source, target, "AspNetUserRoles");
        await CopyEntities<IdentityUserToken<string>>(source, target, "AspNetUserTokens");

        // Reset sequences for tables with integer PKs
        await ResetSequences(target);

        Console.WriteLine("  Auth database migration complete.");
        Console.WriteLine();
    }

    // =========================================================================
    // Progeny Database
    // =========================================================================
    private static async Task MigrateProgenyDatabase()
    {
        Console.WriteLine("--- Migrating Progeny Database ---");

        await using var source = CreateContext<ProgenyDbContext>(SqlServerProgeny, useSqlServer: true);
        await using var target = CreateContext<ProgenyDbContext>(PostgresProgeny, useSqlServer: false);

        // Independent tables first, then tables with FK dependencies
        await CopyEntities<Progeny>(source, target, "Progeny");
        await CopyEntities<UserInfo>(source, target, "UserInfo");
        await CopyEntities<KinaUnaLanguage>(source, target, "Languages");
        await CopyEntities<KinaUnaTextNumber>(source, target, "KinaUnaTextNumbers");
        await CopyEntities<KinaUnaText>(source, target, "KinaUnaTexts");
        await CopyEntities<TextTranslation>(source, target, "TextTranslations");
        await CopyEntities<Address>(source, target, "Addresses");
        await CopyEntities<TimeLineItem>(source, target, "TimeLineItems");
        await CopyEntities<Location>(source, target, "Locations");
        await CopyEntities<CalendarItem>(source, target, "CalendarItems");
        await CopyEntities<VocabularyItem>(source, target, "VocabularyItems");
        await CopyEntities<Skill>(source, target, "Skills");
        await CopyEntities<Friend>(source, target, "Friends");
        await CopyEntities<Measurement>(source, target, "Measurements");
        await CopyEntities<Sleep>(source, target, "Sleep");
        await CopyEntities<Note>(source, target, "Notes");
        await CopyEntities<Contact>(source, target, "Contacts");
        await CopyEntities<Vaccination>(source, target, "Vaccinations");
        await CopyEntities<MobileNotification>(source, target, "MobileNotifications");
        await CopyEntities<WebNotification>(source, target, "WebNotifications");
        await CopyEntities<PushDevices>(source, target, "PushDevices");
        await CopyEntities<KinaUnaBackgroundTask>(source, target, "BackgroundTasks");
        await CopyEntities<CalendarReminder>(source, target, "CalendarReminders");
        await CopyEntities<ProgenyInfo>(source, target, "ProgenyInfo");
        await CopyEntities<RecurrenceRule>(source, target, "RecurrenceRules");
        await CopyEntities<TodoItem>(source, target, "TodoItems");
        await CopyEntities<KanbanBoard>(source, target, "KanbanBoards");
        await CopyEntities<KanbanItem>(source, target, "KanbanItems");
        await CopyEntities<UserGroup>(source, target, "UserGroups");
        await CopyEntities<UserGroupMember>(source, target, "UserGroupMembers");
        await CopyEntities<Family>(source, target, "Families");
        await CopyEntities<FamilyMember>(source, target, "FamilyMembers");
        await CopyEntities<FamilyPermission>(source, target, "FamilyPermissions");
        await CopyEntities<ProgenyPermission>(source, target, "ProgenyPermissions");
        await CopyEntities<TimelineItemPermission>(source, target, "TimelineItemPermissions");
        await CopyEntities<PermissionAuditLog>(source, target, "PermissionAuditLogs");
        await CopyEntities<FamilyAuditLog>(source, target, "FamilyAuditLogs");
        await CopyEntities<UserGroupAuditLog>(source, target, "UserGroupAuditLogs");

        // Reset all integer sequences
        await ResetSequences(target);

        Console.WriteLine("  Progeny database migration complete.");
        Console.WriteLine();
    }

    // =========================================================================
    // Media Database
    // =========================================================================
    private static async Task MigrateMediaDatabase()
    {
        Console.WriteLine("--- Migrating Media Database ---");

        await using var source = CreateContext<MediaDbContext>(SqlServerMedia, useSqlServer: true);
        await using var target = CreateContext<MediaDbContext>(PostgresMedia, useSqlServer: false);

        // CommentThread before Comment (FK dependency)
        await CopyEntities<CommentThread>(source, target, "CommentThreads");
        await CopyEntities<Comment>(source, target, "Comments");
        await CopyEntities<Picture>(source, target, "Pictures");
        await CopyEntities<Video>(source, target, "Videos");

        // Reset sequences
        await ResetSequences(target);

        Console.WriteLine("  Media database migration complete.");
        Console.WriteLine();
    }

    // =========================================================================
    // Helper: Copy entities from source to target in batches
    // =========================================================================
    private static async Task CopyEntities<TEntity>(DbContext source, DbContext target, string displayName, int batchSize = 500)
        where TEntity : class
    {
        try
        {
            var items = await source.Set<TEntity>().AsNoTracking().ToListAsync();

            if (items.Count == 0)
            {
                Console.WriteLine($"  {displayName}: 0 rows (skipped)");
                return;
            }

            // Process in batches to avoid memory issues with large tables
            int processed = 0;
            foreach (var batch in items.Chunk(batchSize))
            {
                // Normalize DateTime values on the objects directly before adding to context
                foreach (var item in batch)
                {
                    NormalizeDateTimesOnEntity(item);
                }

                target.Set<TEntity>().AddRange(batch);
                await target.SaveChangesAsync();

                // Detach all tracked entities to free memory
                foreach (var entry in target.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                processed += batch.Length;
            }

            Console.WriteLine($"  {displayName}: {processed} rows migrated");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {displayName}: ERROR — {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"    Inner: {ex.InnerException.Message}");
            }
            Console.ResetColor();
        }
    }

    // =========================================================================
    // Helper: Normalize all DateTime values on an entity to UTC via reflection
    // =========================================================================
    // SQL Server stores datetime2 with Kind=Unspecified, but Npgsql requires
    // Kind=UTC for 'timestamp with time zone' columns. This method sets the Kind
    // to UTC directly on the object's properties (treating existing values as
    // already being UTC, which is the standard assumption for data stored
    // without timezone info in SQL Server).
    private static void NormalizeDateTimesOnEntity(object entity)
    {
        var type = entity.GetType();

        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            if (prop.PropertyType == typeof(DateTime))
            {
                var value = (DateTime)prop.GetValue(entity)!;
                if (value.Kind == DateTimeKind.Unspecified)
                {
                    prop.SetValue(entity, DateTime.SpecifyKind(value, DateTimeKind.Utc));
                }
            }
            else if (prop.PropertyType == typeof(DateTime?))
            {
                var value = (DateTime?)prop.GetValue(entity);
                if (value.HasValue && value.Value.Kind == DateTimeKind.Unspecified)
                {
                    prop.SetValue(entity, DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
                }
            }
            else if (prop.PropertyType == typeof(DateTimeOffset))
            {
                var value = (DateTimeOffset)prop.GetValue(entity)!;
                if (value.Offset != TimeSpan.Zero)
                {
                    prop.SetValue(entity, value.ToUniversalTime());
                }
            }
            else if (prop.PropertyType == typeof(DateTimeOffset?))
            {
                var value = (DateTimeOffset?)prop.GetValue(entity);
                if (value.HasValue && value.Value.Offset != TimeSpan.Zero)
                {
                    prop.SetValue(entity, value.Value.ToUniversalTime());
                }
            }
        }
    }

    // =========================================================================
    // Helper: Reset all integer identity sequences to max(id) + 1
    // =========================================================================
    private static async Task ResetSequences(DbContext context)
    {
        try
        {
            // Query all sequences and reset them based on the max value in their table
            var resetSql = @"
                DO $$
                DECLARE
                    r RECORD;
                    max_val BIGINT;
                BEGIN
                    FOR r IN
                        SELECT
                            t.table_name,
                            c.column_name,
                            pg_get_serial_sequence(quote_ident(t.table_name), c.column_name) AS seq_name
                        FROM information_schema.tables t
                        JOIN information_schema.columns c
                            ON t.table_name = c.table_name AND t.table_schema = c.table_schema
                        WHERE t.table_schema = 'public'
                          AND t.table_type = 'BASE TABLE'
                          AND c.column_default LIKE 'nextval%'
                    LOOP
                        EXECUTE format('SELECT COALESCE(MAX(%I), 0) FROM %I', r.column_name, r.table_name) INTO max_val;
                        IF max_val > 0 THEN
                            PERFORM setval(r.seq_name, max_val);
                        END IF;
                    END LOOP;
                END $$;";

            await context.Database.ExecuteSqlRawAsync(resetSql);
            Console.WriteLine("  Sequences reset successfully.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  WARNING: Failed to reset sequences — {ex.Message}");
            Console.WriteLine("  You may need to reset sequences manually.");
            Console.ResetColor();
        }
    }

    // =========================================================================
    // Helper: Create a DbContext with the specified provider
    // =========================================================================
    private static TContext CreateContext<TContext>(string connectionString, bool useSqlServer) where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        if (useSqlServer)
        {
            optionsBuilder.UseSqlServer(connectionString, opts =>
            {
                opts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            });
        }
        else
        {
            optionsBuilder.UseNpgsql(connectionString, opts =>
            {
                opts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            });
        }

        // For ApplicationDbContext, we need UseOpenIddict on the target
        if (!useSqlServer && typeof(TContext) == typeof(ApplicationDbContext))
        {
            optionsBuilder.UseOpenIddict();
        }

        return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
    }
}