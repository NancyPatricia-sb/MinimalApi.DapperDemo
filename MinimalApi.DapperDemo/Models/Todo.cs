namespace MinimalApi.DapperDemo.Models;

public record Todo(
    int Id,
    string Title,
    bool IsDone,
    DateTime CreatedAt
);

public record TodoCreateRequest(
    string Title
);

public record TodoUpdateRequest(
    string Title,
    bool IsDone
);