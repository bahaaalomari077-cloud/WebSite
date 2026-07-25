using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using NpgsqlTypes;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
builder.WebHost.UseUrls($"http://localhost:{port}");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "cp-admin";
        options.LoginPath = "/api/admin/login";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var connectionString = BuildConnectionString(builder.Configuration);
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var publicPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "public"));
if (Directory.Exists(publicPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(publicPath),
        RequestPath = ""
    });
}

await EnsureDatabase(app.Services.GetRequiredService<NpgsqlDataSource>());

app.MapGet("/", () => Results.Ok(new { message = "Credit Plus ASP.NET backend is running" }));

app.MapPost("/api/upload", [Authorize] async (HttpRequest request, IWebHostEnvironment environment) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Upload must be multipart/form-data." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files["image"];

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "Choose an image first." });
    }

    var extension = Path.GetExtension(file.FileName);
    var validationError = ValidateImageExtension(extension);
    if (validationError is not null)
    {
        return Results.BadRequest(new { error = validationError });
    }

    const long maxBytes = 5 * 1024 * 1024;
    if (file.Length > maxBytes)
    {
        return Results.BadRequest(new { error = "Image must be 5MB or smaller." });
    }

    var uploadRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "public", "uploads"));
    Directory.CreateDirectory(uploadRoot);

    var baseName = Path.GetFileNameWithoutExtension(file.FileName);
    var safeName = SanitizeFileName(baseName);
    var savedName = $"{safeName}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";
    var savedPath = Path.Combine(uploadRoot, savedName);

    await using var stream = File.Create(savedPath);
    await file.CopyToAsync(stream);

    return Results.Ok(new { img = $"uploads/{savedName}", fileName = savedName });
});

app.MapPost("/api/admin/login", async (HttpContext context, LoginRequest credentials, NpgsqlDataSource dataSource) =>
{
    var storedHash = await GetAdminPasswordHash(dataSource, credentials.username);
    if (storedHash is not null && VerifyPassword(credentials.password, storedHash))
    {
        var claims = new[] { new Claim(ClaimTypes.Name, credentials.username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.Ok(new { success = true });
    }

    return Results.Unauthorized();
});

app.MapPost("/api/admin/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { success = true });
});

app.MapGet("/api/admin/me", [Authorize] (ClaimsPrincipal user) =>
{
    return Results.Ok(new { user = user.Identity?.Name });
});

app.MapGet("/api/news", async (NpgsqlDataSource dataSource) =>
{
    try
    {
        return Results.Ok(await ReadPosts(dataSource, "news"));
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to load news");
        return Results.Problem("Database error", statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/api/news", [Authorize] async (PostRequest post, NpgsqlDataSource dataSource) =>
{
    try
    {
        var validationError = ValidatePost(post);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        await InsertPost(dataSource, "news", post);
        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to create news post");
        if (ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Results.BadRequest(new { error = "ID already exists. Use a different ID." });
        }

        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPut("/api/news/{id}", [Authorize] async (string id, PostRequest post, NpgsqlDataSource dataSource) =>
{
    try
    {
        var validationError = ValidatePost(post);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var updated = await UpdatePost(dataSource, "news", id, post);
        return updated ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "News item not found." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to update news post");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/articles", async (NpgsqlDataSource dataSource) =>
{
    try
    {
        return Results.Ok(await ReadPosts(dataSource, "articles"));
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to load articles");
        return Results.Problem("Database error", statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/api/articles", [Authorize] async (PostRequest post, NpgsqlDataSource dataSource) =>
{
    try
    {
        var validationError = ValidatePost(post);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        await InsertPost(dataSource, "articles", post);
        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to create article");
        if (ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Results.BadRequest(new { error = "ID already exists. Use a different ID." });
        }

        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPut("/api/articles/{id}", [Authorize] async (string id, PostRequest post, NpgsqlDataSource dataSource) =>
{
    try
    {
        var validationError = ValidatePost(post);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var updated = await UpdatePost(dataSource, "articles", id, post);
        return updated ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Article not found." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to update article");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapDelete("/api/news/{id}", [Authorize] async (string id, NpgsqlDataSource dataSource) =>
{
    try
    {
        var deleted = await DeletePost(dataSource, "news", id);
        return deleted ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "News item not found." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to delete news post");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapDelete("/api/articles/{id}", [Authorize] async (string id, NpgsqlDataSource dataSource) =>
{
    try
    {
        var deleted = await DeletePost(dataSource, "articles", id);
        return deleted ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Article not found." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to delete article");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["Database:Host"] ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["Database:Port"] ?? "5433";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["Database:Name"] ?? "postgres";
    var user = Environment.GetEnvironmentVariable("DB_USER") ?? configuration["Database:User"] ?? "postgres";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["Database:Password"] ?? "123456";

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = int.Parse(port),
        Database = database,
        Username = user,
        Password = password
    };

    return builder.ConnectionString;
}

static async Task EnsureDatabase(NpgsqlDataSource dataSource)
{
    await using var command = dataSource.CreateCommand("""
        CREATE TABLE IF NOT EXISTS news (
          id         VARCHAR(50) PRIMARY KEY,
          img        VARCHAR(255),
          date_en    VARCHAR(100),
          date_ar    VARCHAR(100),
          title_en   TEXT,
          title_ar   TEXT,
          blocks_en  JSONB,
          blocks_ar  JSONB,
          sort_order INT DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS articles (
          id         VARCHAR(50) PRIMARY KEY,
          img        VARCHAR(255),
          date_en    VARCHAR(100),
          date_ar    VARCHAR(100),
          title_en   TEXT,
          title_ar   TEXT,
          blocks_en  JSONB,
          blocks_ar  JSONB,
          sort_order INT DEFAULT 0
        );

        ALTER TABLE news     ADD COLUMN IF NOT EXISTS blocks_en JSONB;
        ALTER TABLE news     ADD COLUMN IF NOT EXISTS blocks_ar JSONB;
        ALTER TABLE articles ADD COLUMN IF NOT EXISTS blocks_en JSONB;
        ALTER TABLE articles ADD COLUMN IF NOT EXISTS blocks_ar JSONB;

        INSERT INTO news (id, img, date_en, date_ar, title_en, title_ar, sort_order) VALUES
          ('hb',      'news-hb.jpg',      'July 26, 2025',  '26 تموز 2025',  'Proud to Power Housing Bank''s New Supply Chain Finance Program',                'نفخر بتشغيل برنامج تمويل سلسلة التوريد الجديد لبنك الإسكان', 1),
          ('poc',     'news-poc.jpg',     '2025',           '2025',          'JOPACC & Credit Plus Complete a Successful Proof of Concept with Housing Bank', 'جوباك وكريدت بلس تكملان إثبات مفهوم ناجح مع بنك الإسكان',   2),
          ('seminar', 'news-seminar.jpg', 'April 26, 2026', '26 نيسان 2026', 'Supply Chain Finance Session with the Association of Banks in Jordan',          'جلسة تمويل سلسلة التوريد مع جمعية البنوك في الأردن',         3)
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO articles (id, img, date_en, date_ar, title_en, title_ar, sort_order) VALUES
          ('buyers-scf', 'art-buyers.jpg', 'Insight', 'رؤية', 'Why Corporate Buyers Use Supply Chain Finance Platforms - It''s Time to Eliminate Post-Dated Cheques', 'لماذا يستخدم المشترون من الشركات منصات تمويل سلسلة التوريد؟ حان وقت التخلص من الشيكات المؤجلة', 1),
          ('ccc',        'art-ccc.jpg',    'Insight', 'رؤية', 'What Is the Cash Conversion Cycle (CCC)?',                                                               'ما هي دورة تحويل النقد (CCC)؟',                                                                   2),
          ('pwc',        'art-pwc.jpg',    'Insight', 'رؤية', 'Understanding Supply Chain Finance - by PwC',                                                            'فهم تمويل سلسلة التوريد - عن PwC',                                                                3)
        ON CONFLICT (id) DO NOTHING;

        DELETE FROM news
        WHERE id = 'r'
           OR img = 'k'
           OR id = 'codex-test-save';

        DELETE FROM articles
        WHERE id = 'r'
           OR img = 'k'
           OR id = 'codex-test-save';

        CREATE TABLE IF NOT EXISTS admin_users (
          id            SERIAL PRIMARY KEY,
          username      VARCHAR(50) UNIQUE NOT NULL,
          password_hash VARCHAR(255) NOT NULL,
          created_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        -- Insert default admin if no users exist.
        -- Default password is "admin123" and should be changed immediately.
        INSERT INTO admin_users (username, password_hash)
        SELECT 'admin', @defaultHash
        WHERE NOT EXISTS (SELECT 1 FROM admin_users);
        """);

    command.Parameters.AddWithValue("defaultHash", HashPassword("admin123"));
    await command.ExecuteNonQueryAsync();
}

static string? ValidatePost(PostRequest post)
{
    if (string.IsNullOrWhiteSpace(post.id) ||
        string.IsNullOrWhiteSpace(post.img) ||
        string.IsNullOrWhiteSpace(post.date_en) ||
        string.IsNullOrWhiteSpace(post.date_ar) ||
        string.IsNullOrWhiteSpace(post.title_en) ||
        string.IsNullOrWhiteSpace(post.title_ar))
    {
        return "All main fields are required.";
    }

    var extension = Path.GetExtension(post.img);
    var imageError = ValidateImageExtension(extension);
    if (imageError is not null)
    {
        return imageError;
    }

    return null;
}

static string? ValidateImageExtension(string extension)
{
    var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
    return allowedImageExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
        ? null
        : "Image filename must end with .jpg, .jpeg, .png, .webp, or .svg.";
}

static string SanitizeFileName(string value)
{
    var safeChars = value
        .ToLowerInvariant()
        .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
        .ToArray();
    var safeName = new string(safeChars).Trim('-');
    return string.IsNullOrWhiteSpace(safeName) ? "image" : safeName;
}

static async Task<List<object>> ReadPosts(NpgsqlDataSource dataSource, string tableName)
{
    await using var command = dataSource.CreateCommand($"""
        SELECT id, img, date_en, date_ar, title_en, title_ar, blocks_en::text, blocks_ar::text, sort_order
        FROM {tableName}
        ORDER BY sort_order DESC
        """);

    await using var reader = await command.ExecuteReaderAsync();
    var posts = new List<object>();

    while (await reader.ReadAsync())
    {
        posts.Add(new
        {
            id = reader.GetString(0),
            img = reader.IsDBNull(1) ? null : reader.GetString(1),
            date_en = reader.IsDBNull(2) ? null : reader.GetString(2),
            date_ar = reader.IsDBNull(3) ? null : reader.GetString(3),
            title_en = reader.IsDBNull(4) ? null : reader.GetString(4),
            title_ar = reader.IsDBNull(5) ? null : reader.GetString(5),
            blocks_en = ReadJson(reader, 6),
            blocks_ar = ReadJson(reader, 7),
            sort_order = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
        });
    }

    return posts;
}

static async Task InsertPost(NpgsqlDataSource dataSource, string tableName, PostRequest post)
{
    await using var command = dataSource.CreateCommand($"""
        INSERT INTO {tableName} (id, img, date_en, date_ar, title_en, title_ar, blocks_en, blocks_ar, sort_order)
        VALUES (@id, @img, @date_en, @date_ar, @title_en, @title_ar, @blocks_en, @blocks_ar,
                (SELECT COALESCE(MAX(sort_order), 0) + 1 FROM {tableName}))
        """);

    command.Parameters.AddWithValue("id", post.id);
    command.Parameters.AddWithValue("img", post.img);
    command.Parameters.AddWithValue("date_en", post.date_en);
    command.Parameters.AddWithValue("date_ar", post.date_ar);
    command.Parameters.AddWithValue("title_en", post.title_en);
    command.Parameters.AddWithValue("title_ar", post.title_ar);
    command.Parameters.AddWithValue("blocks_en", NpgsqlDbType.Jsonb, ToJson(post.blocks_en));
    command.Parameters.AddWithValue("blocks_ar", NpgsqlDbType.Jsonb, ToJson(post.blocks_ar));

    await command.ExecuteNonQueryAsync();
}

static async Task<bool> UpdatePost(NpgsqlDataSource dataSource, string tableName, string id, PostRequest post)
{
    await using var command = dataSource.CreateCommand($"""
        UPDATE {tableName}
        SET img = @img,
            date_en = @date_en,
            date_ar = @date_ar,
            title_en = @title_en,
            title_ar = @title_ar,
            blocks_en = @blocks_en,
            blocks_ar = @blocks_ar
        WHERE id = @id
        """);

    command.Parameters.AddWithValue("id", id);
    command.Parameters.AddWithValue("img", post.img);
    command.Parameters.AddWithValue("date_en", post.date_en);
    command.Parameters.AddWithValue("date_ar", post.date_ar);
    command.Parameters.AddWithValue("title_en", post.title_en);
    command.Parameters.AddWithValue("title_ar", post.title_ar);
    command.Parameters.AddWithValue("blocks_en", NpgsqlDbType.Jsonb, ToJson(post.blocks_en));
    command.Parameters.AddWithValue("blocks_ar", NpgsqlDbType.Jsonb, ToJson(post.blocks_ar));

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<bool> DeletePost(NpgsqlDataSource dataSource, string tableName, string id)
{
    await using var command = dataSource.CreateCommand($"""
        DELETE FROM {tableName}
        WHERE id = @id
        """);

    command.Parameters.AddWithValue("id", id);

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<string?> GetAdminPasswordHash(NpgsqlDataSource dataSource, string username)
{
    await using var command = dataSource.CreateCommand("""
        SELECT password_hash FROM admin_users WHERE username = @username
        """);

    command.Parameters.AddWithValue("username", username);
    var result = await command.ExecuteScalarAsync();
    return result is DBNull ? null : (string?)result;
}

static string HashPassword(string password)
{
    var salt = new byte[16];
    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
    {
        rng.GetBytes(salt);
    }

    var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
        password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
    var hash = pbkdf2.GetBytes(32);

    var hashBytes = new byte[48];
    Buffer.BlockCopy(salt, 0, hashBytes, 0, 16);
    Buffer.BlockCopy(hash, 0, hashBytes, 16, 32);
    return Convert.ToBase64String(hashBytes);
}

static bool VerifyPassword(string password, string storedHash)
{
    var hashBytes = Convert.FromBase64String(storedHash);
    var salt = new byte[16];
    Buffer.BlockCopy(hashBytes, 0, salt, 0, 16);

    var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
        password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
    var hash = pbkdf2.GetBytes(32);

    return hash.AsSpan().SequenceEqual(hashBytes.AsSpan(16, 32));
}

static JsonElement? ReadJson(NpgsqlDataReader reader, int ordinal)
{
    if (reader.IsDBNull(ordinal))
    {
        return null;
    }

    return JsonSerializer.Deserialize<JsonElement>(reader.GetString(ordinal));
}

static string ToJson(JsonElement? value)
{
    return value is { ValueKind: not JsonValueKind.Undefined } json
        ? json.GetRawText()
        : "[]";
}

record PostRequest(
    string id,
    string img,
    string date_en,
    string date_ar,
    string title_en,
    string title_ar,
    JsonElement? blocks_en,
    JsonElement? blocks_ar);

record LoginRequest(string username, string password);
