using DevSource.Dispatcher.Behaviors;
using Order.Application.Commands;

namespace Order.Application.Validators;

public sealed class CreateOrderCommandValidator : IRequestValidator<CreateOrderCommand>
{
    public ValueTask<IReadOnlyList<ValidationError>> ValidateAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            errors.Add(new ValidationError(nameof(request.CustomerName), "Customer name is required."));

        if (request.Items.Count == 0)
            errors.Add(new ValidationError(nameof(request.Items), "An order needs at least one item."));

        foreach (var item in request.Items.Where(static item => item.Quantity <= 0))
            errors.Add(new ValidationError(nameof(item.Quantity), $"Quantity of '{item.ProductName}' must be greater than zero."));

        return ValueTask.FromResult<IReadOnlyList<ValidationError>>(errors);
    }
}
