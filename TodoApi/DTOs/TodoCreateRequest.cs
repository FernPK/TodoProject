namespace TodoApi.DTOs;

public class TodoCreateRequest
{
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}