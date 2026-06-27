using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
builder.WebHost.UseUrls($"http://localhost:{port}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var connectionString = BuildConnectionString(builder.Configuration);
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

app.UseCors();

await EnsureDatabase(app.Services.GetRequiredService<NpgsqlDataSource>());

app.MapGet("/", () => Results.Ok(new { message = "Credit Plus ASP.NET backend is running" }));

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

app.MapPost("/api/news", async (PostRequest post, NpgsqlDataSource dataSource) =>
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
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPut("/api/news/{id}", async (string id, PostRequest post, NpgsqlDataSource dataSource) =>
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

app.MapPost("/api/articles", async (PostRequest post, NpgsqlDataSource dataSource) =>
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
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPut("/api/articles/{id}", async (string id, PostRequest post, NpgsqlDataSource dataSource) =>
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
        WHERE id = 'r' AND img = 'k' AND date_en = 'k' AND title_en = 'k';

        DELETE FROM articles
        WHERE id = 'r' AND img = 'k' AND date_en = 'k' AND title_en = 'k';
        """);

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

    var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
    if (!allowedImageExtensions.Any(ext => post.img.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
    {
        return "Image filename must end with .jpg, .jpeg, .png, .webp, or .svg.";
    }

    return null;
}

static async Task<List<object>> ReadPosts(NpgsqlDataSource dataSource, string tableName)
{
    await using var command = dataSource.CreateCommand($"""
        SELECT id, img, date_en, date_ar, title_en, title_ar, blocks_en::text, blocks_ar::text, sort_order
        FROM {tableName}
        ORDER BY sort_order ASC
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
