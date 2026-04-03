using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DevSource.Dispatcher.SourceGenerator;

internal sealed class DispatcherGenerationModel
{
    public DispatcherGenerationModel(
        ImmutableArray<string> commandRequests,
        ImmutableArray<RequestWithResponseModel> commandRequestsWithResponse,
        ImmutableArray<RequestWithResponseModel> queryRequests,
        ImmutableArray<string> notificationRequests)
    {
        CommandRequests = commandRequests;
        CommandRequestsWithResponse = commandRequestsWithResponse;
        QueryRequests = queryRequests;
        NotificationRequests = notificationRequests;
    }

    public ImmutableArray<string> CommandRequests { get; }

    public ImmutableArray<RequestWithResponseModel> CommandRequestsWithResponse { get; }

    public ImmutableArray<RequestWithResponseModel> QueryRequests { get; }

    public ImmutableArray<string> NotificationRequests { get; }

    public static DispatcherGenerationModel Create(Compilation compilation, ImmutableArray<INamedTypeSymbol?> candidateTypes)
    {
        var commandHandlerSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.CommandHandlerMetadataName);
        var commandHandlerWithResponseSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.CommandHandlerWithResponseMetadataName);
        var queryHandlerSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.QueryHandlerMetadataName);
        var notificationHandlerSymbol = compilation.GetTypeByMetadataName(DispatcherSourceGenerator.NotificationHandlerMetadataName);

        var commandRequests = ImmutableSortedSet.CreateBuilder<string>(StringComparer.Ordinal);
        var commandRequestsWithResponse = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        var queryRequests = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        var notificationRequests = ImmutableSortedSet.CreateBuilder<string>(StringComparer.Ordinal);

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
                    continue;
                }

                if (commandHandlerWithResponseSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, commandHandlerWithResponseSymbol))
                {
                    commandRequestsWithResponse[GetTypeName(implementedInterface.TypeArguments[0])] = GetTypeName(implementedInterface.TypeArguments[1]);
                    continue;
                }

                if (queryHandlerSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, queryHandlerSymbol))
                {
                    queryRequests[GetTypeName(implementedInterface.TypeArguments[0])] = GetTypeName(implementedInterface.TypeArguments[1]);
                    continue;
                }

                if (notificationHandlerSymbol is not null && SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, notificationHandlerSymbol))
                    notificationRequests.Add(GetTypeName(implementedInterface.TypeArguments[0]));
            }
        }

        return new DispatcherGenerationModel(
            commandRequests.ToImmutable().ToImmutableArray(),
            commandRequestsWithResponse.Select(static pair => new RequestWithResponseModel(pair.Key, pair.Value)).ToImmutableArray(),
            queryRequests.Select(static pair => new RequestWithResponseModel(pair.Key, pair.Value)).ToImmutableArray(),
            notificationRequests.ToImmutable().ToImmutableArray());
    }

    private static string GetTypeName(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
}