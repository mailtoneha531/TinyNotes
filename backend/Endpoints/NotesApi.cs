using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Endpoints
{
    public static class NotesApi
    {
        public static void MapNotesEndpoints(this WebApplication app)
        {
            var loggerFactory = app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("NotesApi");
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.HuggingFace.json", optional: true, reloadOnChange: true)
                .Build();
            var hfToken = config["HuggingFace:ApiToken"] ?? string.Empty;
            app.MapGet("/api/notes", async (NotesDbContext db, string? search, string? sort) =>
            {
                var query = db.Notes.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(n => n.Title.Contains(search) || n.Content.Contains(search));
                }
                query = sort switch
                {
                    "created" => query.OrderByDescending(n => n.CreatedAt),
                    "updated" => query.OrderByDescending(n => n.UpdatedAt),
                    _ => query.OrderBy(n => n.Id)
                };
                return await query.ToListAsync();
            });

            app.MapGet("/api/notes/{id}", async (NotesDbContext db, int id) =>
                await db.Notes.FindAsync(id) is Note note ? Results.Ok(note) : Results.NotFound());

            app.MapPost("/api/notes", async (NotesDbContext db, Note note) =>
            {
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = DateTime.UtcNow;
                db.Notes.Add(note);
                await db.SaveChangesAsync();
                return Results.Created($"/api/notes/{note.Id}", note);
            });

            app.MapPut("/api/notes/{id}", async (NotesDbContext db, int id, Note updatedNote) =>
            {
                var note = await db.Notes.FindAsync(id);
                if (note is null) return Results.NotFound();
                note.Title = updatedNote.Title;
                note.Content = updatedNote.Content;
                note.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok(note);
            });

            // AI summary and hashtags endpoint using HuggingFace
            app.MapPost("/api/notes/{id}/ai", async (NotesDbContext db, int id) =>
            {
                var note = await db.Notes.FindAsync(id);
                if (note is null) return Results.NotFound();

                // HuggingFace summarization
                var summary = await GetHuggingFaceSummary(note.Content, logger);
                // HuggingFace keyword extraction (for hashtags)
                var hashtags = await GetHuggingFaceKeywords(note.Content, logger);

                // Fallback logic
                if (string.IsNullOrWhiteSpace(summary) || summary == "Summary unavailable.")
                {
                    logger.LogWarning($"AI summary unavailable for note {note.Id}, using fallback.");
                    summary = note.Content.Length > 100 ? note.Content.Substring(0, 100) + "..." : note.Content;
                }
                if (string.IsNullOrWhiteSpace(hashtags))
                {
                    logger.LogWarning($"AI hashtags unavailable for note {note.Id}, using fallback.");
                    hashtags = string.Join(" ", note.Content.Split(' ').Where(w => w.Length > 4).Take(3).Select(w => "#" + w.ToLower()));
                }

                note.Summary = summary;
                note.Hashtags = hashtags;
                await db.SaveChangesAsync();
                return Results.Ok(new { note.Summary, note.Hashtags });
            });

            // Helper methods for HuggingFace API
            async Task<string> GetHuggingFaceSummary(string text, Microsoft.Extensions.Logging.ILogger logger)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {hfToken}");
                var payload = new { inputs = text };
                var response = await client.PostAsJsonAsync("https://api-inference.huggingface.co/models/sshleifer/distilbart-cnn-12-6", payload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    var isHtml = errorMsg.TrimStart().StartsWith("<");
                    var logMsg = isHtml ? $"HuggingFace summary API failed: {response.StatusCode} - Response was HTML (likely model deprecated or unavailable)" : $"HuggingFace summary API failed: {response.StatusCode} - {errorMsg.Split('\n')[0]}";
                    logger.LogError(logMsg);
                    return "Summary unavailable.";
                }
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var summary = root[0].GetProperty("summary_text").GetString();
                    return summary ?? "Summary unavailable.";
                }
                logger.LogError($"HuggingFace summary API returned unexpected format: {json.Split('\n')[0]}");
                return "Summary unavailable.";
            }

            async Task<string> GetHuggingFaceKeywords(string text, Microsoft.Extensions.Logging.ILogger logger)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {hfToken}");
                var payload = new { inputs = text };
                var response = await client.PostAsJsonAsync("https://api-inference.huggingface.co/models/ml6team/keyphrase-extraction-kbir-inspec", payload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    var isHtml = errorMsg.TrimStart().StartsWith("<");
                    var logMsg = isHtml ? $"HuggingFace keywords API failed: {response.StatusCode} - Response was HTML (likely model deprecated or unavailable)" : $"HuggingFace keywords API failed: {response.StatusCode} - {errorMsg.Split('\n')[0]}";
                    logger.LogError(logMsg);
                    return "";
                }
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var keywords = root[0].GetProperty("keywords");
                    if (keywords.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var tags = keywords.EnumerateArray().Select(k => "#" + k.GetString()?.Replace(" ", "").ToLower()).Where(s => !string.IsNullOrEmpty(s));
                        return string.Join(" ", tags);
                    }
                }
                logger.LogError($"HuggingFace keywords API returned unexpected format: {json.Split('\n')[0]}");
                return "";
            }

            app.MapDelete("/api/notes/{id}", async (NotesDbContext db, int id) =>
            {
                var note = await db.Notes.FindAsync(id);
                if (note is null) return Results.NotFound();
                db.Notes.Remove(note);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }
}
