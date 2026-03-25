# Event Sourcing + CQRS Architecture Project

A production-ready Event Sourcing and CQRS (Command Query Responsibility Segregation) implementation using .NET 8, React TypeScript, PostgreSQL, Azure Cosmos DB, and Azure Service Bus.

## 🏗️ Architecture Overview

This project implements a complete Event Sourcing + CQRS architecture following Clean Architecture principles:

### **Write Side (Command Side)**
- **PostgreSQL** - Event Store (source of truth)
- **Outbox Pattern** - Reliable event publishing
- **.NET 8 Web API** - Command processing

### **Read Side (Query Side)**
- **Azure Cosmos DB** - Denormalized read models
- **.NET 8 Web API** - Query processing
- **Redis Cache** - Performance optimization

### **Messaging & Integration**
- **Azure Service Bus** - Asynchronous messaging
- **Background Workers** - Outbox publisher and projection processing

### **Frontend**
- **React + TypeScript** - Modern web application
- **TanStack Query** - Efficient data fetching
- **Microsoft Authentication Library (MSAL)** - Authentication

## 📁 Project Structure

```
Event.Sourcing/
├── src/                                    # Source code
│   ├── BuildingBlocks/                     # Shared components
│   │   ├── EventSourcing.BuildingBlocks.Domain/
│   │   ├── EventSourcing.BuildingBlocks.Application/
│   │   ├── EventSourcing.BuildingBlocks.Infrastructure/
│   │   └── EventSourcing.BuildingBlocks.Contracts/
│   ├── Services/                           # Microservices
│   │   ├── Command/                        # Write side services
│   │   │   ├── EventSourcing.Command.Api/
│   │   │   ├── EventSourcing.Command.Application/
│   │   │   ├── EventSourcing.Command.Domain/
│   │   │   └── EventSourcing.Command.Infrastructure/
│   │   ├── Query/                          # Read side services
│   │   │   ├── EventSourcing.Query.Api/
│   │   │   ├── EventSourcing.Query.Application/
│   │   │   └── EventSourcing.Query.Infrastructure/
│   │   └── Workers/                        # Background services
│   │       ├── EventSourcing.OutboxPublisher/
│   │       └── EventSourcing.ProjectionWorkers/
│   └── Frontend/                           # Client applications
│       └── react-frontend/
├── tests/                                  # Test projects
│   ├── Unit/                              # Unit tests
│   ├── Integration/                       # Integration tests
│   ├── Contract/                          # Contract tests
│   └── EndToEnd/                          # End-to-end tests
├── infra/                                 # Infrastructure as Code
│   ├── terraform/                         # Terraform modules
│   ├── docker/                            # Docker configurations
│   └── scripts/                           # Deployment scripts
└── docs/                                  # Documentation
    └── architecture.md                    # Detailed architecture guide
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js LTS](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### 1. Clone and Build

```bash
# Clone the repository
git clone <repository-url>
cd Event.Sourcing

# Restore and build the solution
dotnet restore
dotnet build

# Run tests
dotnet test
```

### 2. Local Development Environment

```bash
# Start local infrastructure (PostgreSQL, Redis, Cosmos DB Emulator)
docker-compose up -d

# Apply database migrations
dotnet ef database update --project src/Services/Command/EventSourcing.Command.Infrastructure

# Setup Cosmos DB containers
# (Run the setup script once)
```

### 3. Run Services

```bash
# Terminal 1 - Command API
cd src/Services/Command/EventSourcing.Command.Api
dotnet run

# Terminal 2 - Query API
cd src/Services/Query/EventSourcing.Query.Api
dotnet run

# Terminal 3 - Outbox Publisher
cd src/Services/Workers/EventSourcing.OutboxPublisher
dotnet run

# Terminal 4 - Projection Workers
cd src/Services/Workers/EventSourcing.ProjectionWorkers
dotnet run
```

### 4. Frontend Application

```bash
cd src/Frontend/react-frontend
npm install
npm start
```

## 🔧 Configuration

### Environment Variables

Create appropriate `appsettings.Development.json` files in each service:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=eventsourcing;Username=app;Password=app",
    "CosmosDB": "AccountEndpoint=https://localhost:8081/;AccountKey=<emulator-key>",
    "ServiceBus": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<key>",
    "Redis": "localhost:6379"
  },
  "EventStore": {
    "StreamBatchSize": 100,
    "SnapshotFrequency": 50
  },
  "Outbox": {
    "BatchSize": 10,
    "PollingInterval": "00:00:05",
    "MaxRetries": 3
  }
}
```

## 🏛️ Architectural Principles

This project strictly follows these principles as defined in [CLAUDE.md](./CLAUDE.md):

### **Event Sourcing Rules**
- Events are **immutable** and always in **past tense**
- PostgreSQL event store is the **single source of truth**
- Read models are **rebuildable** from events
- Never update or delete events - only append new ones

### **CQRS Separation**
- **Command Side**: Handles business logic, writes to PostgreSQL
- **Query Side**: Optimized for reads, uses Cosmos DB projections
- Complete separation of write and read concerns

### **Outbox Pattern**
- Events and outbox messages written in same transaction
- Background worker publishes events reliably
- No direct publishing from request handlers

### **Eventual Consistency**
- Read side is eventually consistent with write side
- Projections handle updates idempotently
- Graceful handling of messaging failures

## 📊 Key Technologies

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Event Store** | PostgreSQL + Entity Framework | Append-only event storage |
| **Read Models** | Azure Cosmos DB | Scalable, denormalized projections |
| **Messaging** | Azure Service Bus | Reliable pub/sub messaging |
| **Caching** | Redis | Performance optimization |
| **Backend APIs** | .NET 8 + ASP.NET Core | Command/Query processing |
| **Frontend** | React + TypeScript | Modern web interface |
| **Authentication** | Microsoft Entra ID + MSAL | Identity and access management |
| **Observability** | Application Insights + Serilog | Logging and monitoring |
| **Testing** | xUnit + TestContainers | Comprehensive test suite |

## 🧪 Testing Strategy

### Test Categories
- **Unit Tests**: Domain logic, aggregates, command/query handlers
- **Integration Tests**: Database operations, event store, projections
- **Contract Tests**: API contracts, event schema compatibility
- **End-to-End Tests**: Complete user workflows

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deployment

### Azure Container Apps
The application is designed to run on Azure Container Apps with:

- **Command API**: Handles write operations
- **Query API**: Handles read operations
- **Outbox Publisher**: Background worker for event publishing
- **Projection Workers**: Background workers for read model updates

### Infrastructure as Code
Use Terraform modules in the `infra/` directory:

```bash
cd infra/terraform
terraform init
terraform plan
terraform apply
```

## 📚 Documentation

- [**CLAUDE.md**](./CLAUDE.md) - Complete architectural guidelines and coding rules
- [**architecture.md**](./docs/architecture.md) - Detailed technical architecture
- [**API Documentation**](./docs/api-design.md) - REST API specifications
- [**Event Catalog**](./docs/event-catalog.md) - Domain events reference

## 🤝 Development Workflow

1. **Follow CLAUDE.md rules** - Strict architectural guidelines
2. **Clean Architecture** - Dependency inversion and layer separation
3. **Test-Driven Development** - Write tests before implementation
4. **Event-First Design** - Model business processes as domain events
5. **Idempotent Operations** - Handle duplicate processing gracefully

## 🔍 Health Checks

All services expose health check endpoints:

- Command API: `https://localhost:5001/health`
- Query API: `https://localhost:5002/health`
- Workers: Kubernetes-style health probes

## 📈 Performance Considerations

- **Event Store Optimization**: Proper indexing and snapshot strategy
- **Projection Efficiency**: Batch processing and parallel workers
- **Caching Strategy**: Redis for hot data and computed results
- **Connection Pooling**: Optimized database connections

## 🛠️ Troubleshooting

### Common Issues

1. **Database Connection Errors**: Check PostgreSQL container status
2. **Cosmos DB Connectivity**: Verify emulator or Azure connection strings
3. **Service Bus Issues**: Ensure topic/subscription configuration
4. **Pipeline Failures**: Check background worker health

### Useful Commands

```bash
# Check service status
docker-compose ps

# View logs
docker-compose logs -f postgres
docker-compose logs -f redis

# Reset local environment
docker-compose down -v
docker-compose up -d
```

## 📧 Support

For questions and support:
- Review [CLAUDE.md](./CLAUDE.md) for architectural guidance
- Check [architecture.md](./docs/architecture.md) for technical details
- Create issues for bugs or feature requests

---

**Built with ❤️ using Event Sourcing + CQRS architecture**
