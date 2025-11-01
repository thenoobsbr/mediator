# TheNoobs.Mediator

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/TheNoobs.Mediator.svg)](https://www.nuget.org/packages/TheNoobs.Mediator/)
[![License](https://img.shields.io/github/license/thenoobsbr/mediator)](https://github.com/thenoobsbr/mediator/blob/main/LICENSE)

Uma implementação leve e eficiente do padrão Mediator para .NET, construída sobre `TheNoobs.Results` para tratamento robusto de erros.

## 📋 Índice

- [Características](#-características)
- [Instalação](#-instalação)
- [Início Rápido](#-início-rápido)
- [Conceitos](#-conceitos)
- [Uso Avançado](#-uso-avançado)
- [Exemplos](#-exemplos)
- [Contribuindo](#-contribuindo)

## ✨ Características

- **Leve e performático**: Implementação minimalista focada em desempenho
- **Type-safe**: Totalmente tipado com suporte a genéricos
- **Pipeline de handlers**: Suporte a middlewares/pipelines para cross-cutting concerns
- **Event handling**: Suporte nativo para publicação de eventos
- **Injeção de dependência**: Integração perfeita com `Microsoft.Extensions.DependencyInjection`
- **Result pattern**: Integrado com `TheNoobs.Results` para tratamento de erros funcional
- **.NET 9.0**: Construído para a versão mais recente do .NET

## 📦 Instalação

Instale via NuGet Package Manager:

```bash
dotnet add package TheNoobs.Mediator
```

Ou via Package Manager Console:

```powershell
Install-Package TheNoobs.Mediator
```

## 🚀 Início Rápido

### 1. Registre o Mediator

No seu `Program.cs` ou `Startup.cs`:

```csharp
using TheNoobs.Mediator.DependencyInjection;

// Registre o mediator e escaneie assemblies para handlers
builder.Services.AddMediator(typeof(Program).Assembly);
```

### 2. Defina um Command (Interface)

```csharp
public interface ICreateUserCommand
{
    string Name { get; }
    string Email { get; }
}

public record CreateUserCommand(string Name, string Email) : ICreateUserCommand;
```

### 3. Crie um Handler

```csharp
using TheNoobs.Mediator.Abstractions;
using TheNoobs.Results;

public class CreateUserHandler : IHandler<ICreateUserCommand, User>
{
    private readonly IUserRepository _repository;

    public CreateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<User>> HandleAsync(
        ICreateUserCommand command, 
        CancellationToken cancellationToken)
    {
        var user = new User(command.Name, command.Email);
        await _repository.SaveAsync(user, cancellationToken);
        return user;
    }
}
```

### 4. Use o Mediator

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserCommand command)
    {
        var result = await _mediator.SendAsync<ICreateUserCommand, User>(
            command, 
            HttpContext.RequestAborted);

        return result.Match(
            success => Ok(success),
            fail => BadRequest(fail)
        );
    }
}
```

## 📚 Conceitos

### Commands e Handlers

**Commands** devem ser definidos como **interfaces** (requisito do mediator). Os handlers implementam `IHandler<TCommand, TResult>`:

```csharp
public interface IUpdateProductCommand
{
    Guid Id { get; }
    string Name { get; }
    decimal Price { get; }
}

public class UpdateProductHandler : IHandler<IUpdateProductCommand, Product>
{
    public async ValueTask<Result<Product>> HandleAsync(
        IUpdateProductCommand command, 
        CancellationToken cancellationToken)
    {
        // Lógica de atualização
        return updatedProduct;
    }
}
```

### Events e Event Handlers

Para eventos assíncronos, use `IEventHandler<TEvent>`:

```csharp
public record UserCreatedEvent(Guid UserId, string Email);

public class SendWelcomeEmailHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;

    public SendWelcomeEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async ValueTask<Result<Void>> HandleAsync(
        UserCreatedEvent @event, 
        CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(@event.Email);
        return Void.Value;
    }
}
```

Publique eventos:

```csharp
await _mediator.PublishAsync(
    new UserCreatedEvent(user.Id, user.Email), 
    cancellationToken);
```

### Handler Pipelines

Pipelines permitem adicionar comportamentos transversais (logging, validação, transações, etc.):

```csharp
public class LoggingPipeline<TCommand, TResult> : IHandlerPipeline<TCommand, TResult>
    where TCommand : notnull
    where TResult : notnull
{
    private readonly ILogger<LoggingPipeline<TCommand, TResult>> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline<TCommand, TResult>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<Result<TResult>> HandleAsync(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TResult>>> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing command {CommandType}", typeof(TCommand).Name);
        
        var result = await next(command, cancellationToken);
        
        _logger.LogInformation("Command {CommandType} completed", typeof(TCommand).Name);
        
        return result;
    }
}
```

Os pipelines são automaticamente descobertos e registrados pelo `AddMediator()`.

## 🔧 Uso Avançado

### Pipeline de Validação

```csharp
public class ValidationPipeline<TCommand, TResult> : IHandlerPipeline<TCommand, TResult>
    where TCommand : notnull
    where TResult : notnull
{
    private readonly IValidator<TCommand> _validator;

    public ValidationPipeline(IValidator<TCommand> validator)
    {
        _validator = validator;
    }

    public async ValueTask<Result<TResult>> HandleAsync(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TResult>>> next,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command);
        
        if (!validationResult.IsValid)
        {
            return new ValidationFail(validationResult.Errors);
        }

        return await next(command, cancellationToken);
    }
}
```

### Pipeline de Transação

```csharp
public class TransactionPipeline<TCommand, TResult> : IHandlerPipeline<TCommand, TResult>
    where TCommand : notnull
    where TResult : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionPipeline(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<TResult>> HandleAsync(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TResult>>> next,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next(command, cancellationToken);

            if (result.IsSuccess)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### Múltiplos Assemblies

```csharp
services.AddMediator(
    typeof(Program).Assembly,
    typeof(DomainHandler).Assembly,
    typeof(ApplicationHandler).Assembly
);
```

## 📖 Exemplos

### Exemplo Completo: CQRS com Mediator

```csharp
// Command
public interface IPlaceOrderCommand
{
    Guid CustomerId { get; }
    List<OrderItem> Items { get; }
}

public record PlaceOrderCommand(Guid CustomerId, List<OrderItem> Items) : IPlaceOrderCommand;

// Handler
public class PlaceOrderHandler : IHandler<IPlaceOrderCommand, Order>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;

    public PlaceOrderHandler(IOrderRepository orderRepository, IMediator mediator)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
    }

    public async ValueTask<Result<Order>> HandleAsync(
        IPlaceOrderCommand command, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(command.CustomerId, command.Items);
        
        await _orderRepository.SaveAsync(order, cancellationToken);
        
        // Publica evento
        await _mediator.PublishAsync(
            new OrderPlacedEvent(order.Id, order.CustomerId), 
            cancellationToken);
        
        return order;
    }
}

// Event Handler
public class OrderPlacedNotificationHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly INotificationService _notificationService;

    public OrderPlacedNotificationHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async ValueTask<Result<Void>> HandleAsync(
        OrderPlacedEvent @event, 
        CancellationToken cancellationToken)
    {
        await _notificationService.NotifyOrderPlacedAsync(@event.OrderId);
        return Void.Value;
    }
}
```

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença especificada no arquivo [LICENSE](LICENSE).

## 🔗 Links

- [Repositório GitHub](https://github.com/thenoobsbr/mediator)
- [NuGet Package](https://www.nuget.org/packages/TheNoobs.Mediator/)
- [TheNoobs.Results](https://www.nuget.org/packages/TheNoobs.Results/)

---
> ♥ Made with love by The Noobs!
