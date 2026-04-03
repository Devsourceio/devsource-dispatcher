using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DevSource.Dispatcher.SourceGenerator;

internal sealed class DispatcherGenerationModel
{
    public DispatcherGenerationModel(
        ImmutableArray<string> commandRequests,
        ImmutableArray<RequestWithResponseModel> commandRequestsWithResponse,
        ImmutableArray<RequestWithResponseModel> queryRequests,
        ImmutableArray<string> notificationRequests,
        ImmutableArray<ServiceRegistrationModel> serviceRegistrations)
    {
        CommandRequests = commandRequests;
        CommandRequestsWithResponse = commandRequestsWithResponse;
        QueryRequests = queryRequests;
        NotificationRequests = notificationRequests;
        ServiceRegistrations = serviceRegistrations;
    }

    public ImmutableArray<string> CommandRequests { get; }

    public ImmutableArray<RequestWithResponseModel> CommandRequestsWithResponse { get; }

    public ImmutableArray<RequestWithResponseModel> QueryRequests { get; }

    public ImmutableArray<string> NotificationRequests { get; }

    public ImmutableArray<ServiceRegistrationModel> ServiceRegistrations { get; }

    public static DispatcherGenerationModel Create(Compilation compilation, ImmutableArray<INamedTypeSymbol?> candidateTypes)
    {
        var commandHandlerSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.CommandHandlerMetadataName);
        var commandHandlerWithResponseSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.CommandHandlerWithResponseMetadataName);
        var queryHandlerSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.QueryHandlerMetadataName);
        var notificationHandlerSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.NotificationHandlerMetadataName);
        var commandBehaviorSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.CommandPipelineBehaviorMetadataName);
        var behaviorSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.PipelineBehaviorMetadataName);

        var commandRequests = ImmutableSortedSet.CreateBuilder<string>(StringComparer.Ordinal);
        var commandRequestsWithResponse = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        var queryRequests = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        var notificationRequests = ImmutableSortedSet.CreateBuilder<string>(StringComparer.Ordinal);
        var serviceRegistrations = ImmutableDictionary.CreateBuilder<string, ServiceRegistrationModel>(StringComparer.Ordinal);

        foreach (var type in candidateTypes)
        {
            if (type is null || type.IsAbstract)
                continue;

            foreach (var implementedInterface in type.AllInterfaces)
            {
                if (!implementedInterface.IsGenericType)
                    continue;

                if (commandHandlerSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, commandHandlerSymbol))
                {
                    commandRequests.Add(GetTypeName(implementedInterface.TypeArguments[0]));
                    AddRegistration(serviceRegistrations, implementedInterface, type);
                    continue;
                }

                if (commandHandlerWithResponseSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, commandHandlerWithResponseSymbol))
                {
                    commandRequestsWithResponse[GetTypeName(implementedInterface.TypeArguments[0])] = GetTypeName(implementedInterface.TypeArguments[1]);
                    AddRegistration(serviceRegistrations, implementedInterface, type);
                    continue;
                }

                if (queryHandlerSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, queryHandlerSymbol))
                {
                    queryRequests[GetTypeName(implementedInterface.TypeArguments[0])] = GetTypeName(implementedInterface.TypeArguments[1]);
                    AddRegistration(serviceRegistrations, implementedInterface, type);
                    continue;
                }

                if (notificationHandlerSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, notificationHandlerSymbol))
                {
                    notificationRequests.Add(GetTypeName(implementedInterface.TypeArguments[0]));

                    AddRegistration(serviceRegistrations, implementedInterface, type);
                    continue;
                }

                if (commandBehaviorSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, commandBehaviorSymbol))
                {
                    AddRegistration(serviceRegistrations, implementedInterface, type);
                    continue;
                }

                if (behaviorSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, behaviorSymbol))
                    AddRegistration(serviceRegistrations, implementedInterface, type);
            }
        }

        return new DispatcherGenerationModel(
            commandRequests.ToImmutable().ToImmutableArray(),
            commandRequestsWithResponse.Select(static pair => new RequestWithResponseModel(pair.Key, pair.Value)).ToImmutableArray(),
            queryRequests.Select(static pair => new RequestWithResponseModel(pair.Key, pair.Value)).ToImmutableArray(),
            notificationRequests.ToImmutable().ToImmutableArray(),
            serviceRegistrations.Values.OrderBy(static registration => registration.ServiceType, StringComparer.Ordinal)
                .ThenBy(static registration => registration.ImplementationType, StringComparer.Ordinal)
                .ToImmutableArray());
    }

    private static void AddRegistration(
        IDictionary<string, ServiceRegistrationModel> registrations,
        INamedTypeSymbol serviceType,
        INamedTypeSymbol implementationType)
    {
        var isOpenGeneric = IsOpenGeneric(serviceType) || IsOpenGeneric(implementationType);
        var registration = new ServiceRegistrationModel(
            isOpenGeneric ? GetUnboundGenericTypeName(serviceType) : GetTypeName(serviceType),
            isOpenGeneric ? GetUnboundGenericTypeName(implementationType) : GetTypeName(implementationType),
            isOpenGeneric);

        registrations[$"{registration.ServiceType}|{registration.ImplementationType}"] = registration;
    }

    private static string GetTypeName(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));

    private static bool IsOpenGeneric(INamedTypeSymbol symbol)
        => symbol.IsUnboundGenericType || symbol.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter);

    private static string GetUnboundGenericTypeName(INamedTypeSymbol symbol)
        => symbol.OriginalDefinition.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
}
