namespace HouseofCat.RabbitMQ;

public interface IPublishReceipt
{
    bool IsError { get; set; }
    string MessageId { get; set; }

    IMessage GetOriginalMessage();
}

public struct PublishReceipt : IPublishReceipt
{
    public bool IsError { get; set; }
    public string MessageId { get; set; }
    public IMessage OriginalLetter { get; set; }

    public readonly IMessage GetOriginalMessage() => OriginalLetter;
}
