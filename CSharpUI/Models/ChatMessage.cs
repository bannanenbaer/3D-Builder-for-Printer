namespace ThreeDBuilder.Models;

public class ChatMessage
{
    public string Text   { get; set; } = "";
    public bool   IsUser { get; set; }
    public bool   IsBot  => !IsUser;
}
