#!/bin/bash

# Event Sourcing Development Environment Setup Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_message() {
    echo -e "${2:-$GREEN}$1${NC}"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info &> /dev/null; then
        print_message "❌ Docker is not running. Please start Docker Desktop and try again." $RED
        exit 1
    fi
    print_message "✅ Docker is running"
}

# Function to start the development environment
start_environment() {
    print_message "🚀 Starting Event Sourcing development environment..." $BLUE

    # Pull latest images
    print_message "📥 Pulling latest Docker images..."
    docker-compose pull

    # Start services
    print_message "🔧 Starting services..."
    docker-compose up -d

    # Wait for services to be healthy
    print_message "⏳ Waiting for services to be ready..."
    sleep 10

    # Check service health
    print_message "🏥 Checking service health..."

    # Wait for PostgreSQL
    until docker-compose exec postgres pg_isready -U app -d eventsourcing &> /dev/null; do
        echo "Waiting for PostgreSQL..."
        sleep 2
    done
    print_message "✅ PostgreSQL is ready"

    # Wait for Redis
    until docker-compose exec redis redis-cli ping &> /dev/null; do
        echo "Waiting for Redis..."
        sleep 2
    done
    print_message "✅ Redis is ready"

    print_message "🎉 Development environment is ready!" $GREEN
}

# Function to show service information
show_services() {
    print_message "\n📋 Service Information:" $BLUE
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    print_message "🗄️  PostgreSQL:" $YELLOW
    echo "   URL: postgresql://app:app_password_dev@localhost:5432/eventsourcing"
    echo "   Admin: http://localhost:5050 (pgAdmin)"
    echo "   User: admin@eventsourcing.dev / admin_password_dev"

    print_message "🔥 Redis:" $YELLOW
    echo "   URL: redis://localhost:6379"
    echo "   Admin: http://localhost:8082 (Redis Commander)"

    print_message "🌌 Cosmos DB Emulator:" $YELLOW
    echo "   URL: https://localhost:8081"
    echo "   Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

    print_message "🐰 RabbitMQ:" $YELLOW
    echo "   Management: http://localhost:15672"
    echo "   User: dev / dev_123"

    print_message "📝 Seq (Logging):" $YELLOW
    echo "   Web UI: http://localhost:5341"

    print_message "📧 MailHog (Email Testing):" $YELLOW
    echo "   Web UI: http://localhost:8025"
    echo "   SMTP: localhost:1025"

    print_message "🔍 Jaeger (Tracing):" $YELLOW
    echo "   Web UI: http://localhost:16686"

    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

# Function to show application commands
show_app_commands() {
    print_message "\n🛠️  Next Steps:" $BLUE
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    print_message "1. Build the solution:" $YELLOW
    echo "   dotnet restore"
    echo "   dotnet build"

    print_message "2. Run database migrations:" $YELLOW
    echo "   dotnet ef database update --project src/Services/Command/EventSourcing.Command.Infrastructure"

    print_message "3. Start the APIs:" $YELLOW
    echo "   # Command API"
    echo "   dotnet run --project src/Services/Command/EventSourcing.Command.Api"
    echo "   "
    echo "   # Query API (in another terminal)"
    echo "   dotnet run --project src/Services/Query/EventSourcing.Query.Api"

    print_message "4. Start workers:" $YELLOW
    echo "   # Outbox Publisher"
    echo "   dotnet run --project src/Services/Workers/EventSourcing.OutboxPublisher"
    echo "   "
    echo "   # Projection Workers"
    echo "   dotnet run --project src/Services/Workers/EventSourcing.ProjectionWorkers"

    print_message "5. Start the React frontend:" $YELLOW
    echo "   cd src/Frontend/react-frontend"
    echo "   npm install"
    echo "   npm start"

    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

# Function to stop the development environment
stop_environment() {
    print_message "🛑 Stopping development environment..." $YELLOW
    docker-compose down
    print_message "✅ Development environment stopped" $GREEN
}

# Function to clean up everything
cleanup_environment() {
    print_message "🧹 Cleaning up development environment..." $YELLOW
    read -p "This will remove all containers, volumes, and data. Are you sure? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker-compose down -v --remove-orphans
        docker system prune -f
        print_message "✅ Development environment cleaned up" $GREEN
    else
        print_message "❌ Cleanup cancelled" $YELLOW
    fi
}

# Main script logic
case "${1:-start}" in
    start)
        check_docker
        start_environment
        show_services
        show_app_commands
        ;;
    stop)
        stop_environment
        ;;
    restart)
        stop_environment
        sleep 2
        check_docker
        start_environment
        show_services
        ;;
    status)
        docker-compose ps
        ;;
    logs)
        docker-compose logs -f ${2:-}
        ;;
    clean)
        cleanup_environment
        ;;
    info)
        show_services
        show_app_commands
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|logs [service]|clean|info}"
        echo ""
        echo "Commands:"
        echo "  start    - Start the development environment (default)"
        echo "  stop     - Stop all services"
        echo "  restart  - Stop and start services"
        echo "  status   - Show status of all services"
        echo "  logs     - Show logs (optionally for specific service)"
        echo "  clean    - Remove all containers and volumes"
        echo "  info     - Show service URLs and commands"
        exit 1
        ;;
esac
