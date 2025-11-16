namespace Application.ReponseDTO;

public record struct Lookup<T>(T Value, string Text);
