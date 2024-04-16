public class Packet
{
     public string Id { get; set; }
    public string Time { get; set; }
    public string Source { get; set; }
    public string Destination { get; set; }
    public string Protocol { get; set; }
    public string Length { get; set; }
    public string Content { get; set; }

    public Packet(int Id,string time, string source, string destination, string protocol, string length, string content)
    {
        Id = Id;
        Time = time;
        Source = source;
        Destination = destination;
        Protocol = protocol;
        Length = length;
        Content = content;
    }
}
