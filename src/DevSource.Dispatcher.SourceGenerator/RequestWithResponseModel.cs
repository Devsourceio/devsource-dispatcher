namespace DevSource.Dispatcher.SourceGenerator;
internal sealed class RequestWithResponseModel
{
    public RequestWithResponseModel(string requestType, string responseType)
    {
        RequestType = requestType;
        ResponseType = responseType;
    }

    public string RequestType { get; }

    public string ResponseType { get; }
}
    