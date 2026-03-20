namespace GatewayHost.Modules.Api.Features.BookFeature;

public record CreateBookRequest(string Title, string Author);

public static class CreateBookEndpoint
{
    public static IEndpointRouteBuilder MapCreateBookEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/books", async (CreateBookRequest req) =>
        {
            // Minimal demo implementation — replace with real logic
            var created = new { id = 1, title = req.Title, author = req.Author };
            return Results.Created($"/api/books/{created.id}", created);
        })
        .WithName("CreateBook")
        .WithTags("Books");

        return endpoints;
    }
}
