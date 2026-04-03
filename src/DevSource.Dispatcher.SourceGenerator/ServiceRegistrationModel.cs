namespace DevSource.Dispatcher.SourceGenerator;

internal sealed class ServiceRegistrationModel
{
    public ServiceRegistrationModel(string serviceType, string implementationType, bool isOpenGeneric)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        IsOpenGeneric = isOpenGeneric;
    }

    public string ServiceType { get; }

    public string ImplementationType { get; }

    public bool IsOpenGeneric { get; }
}
