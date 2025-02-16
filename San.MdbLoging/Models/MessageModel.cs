
namespace San.MDbLogging.Models;

public class MessageModel
{
    public string Type { get; set; }

    public List<object> Input { get; set; }

    public object Output { get; set; }

    public string MethodName { get; set; }

    public string ClassName { get; set; }
}
