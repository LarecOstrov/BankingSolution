# Banking Solution

## Overview
Banking Solution is a .NET 9-based system that provides a robust banking API with account management, transaction handling, and real-time notifications using WebSockets. The system is designed with a microservices approach, consisting of an API service and a worker service for background transaction processing.

## Features
- User account management (creation, retrieval, listing)
- Secure authentication and role-based access control
- Transaction operations: deposit, withdrawal, and transfers
- Kafka-based asynchronous transaction processing
- Redis caching for fast access to balances
- WebSockets for real-time transaction updates
- Admin features through GraphQL API
- Dockerized deployment with `docker-compose`

## Architecture

The system follows a microservices-based architecture, with separate components handling different aspects of the banking workflow. The key components are:

### 1. API Service

Exposes RESTful endpoints for user authentication, account management, and transactions.
Provides a GraphQL interface for administrative functions.
Manages WebSocket connections for real-time transaction notifications.
Sends transaction requests to the Kafka queue for asynchronous processing.

### 2. Worker Service

Listens to the Kafka queue for incoming transactions.
Processes transactions, updating the database and ensuring consistency.
Updates Redis cache with the latest account balances for fast retrieval.
Sends transaction results back to Kafka for the API to consume and notify users.

### 3. Database (MS SQL Server)

Stores account details, transactions, and balance history.
Ensures ACID compliance for financial operations.
Indexed for optimized read and write performance.

### 4. Message Queue (Kafka)

Provides asynchronous transaction processing, improving system scalability.
Ensures message durability and fault tolerance.
Allows event-driven communication between services.

### 5. Caching Layer (Redis)

Stores frequently accessed account balances to reduce database load.
Ensures fast retrieval of data for real-time API responses.

### 6. WebSockets

Enables real-time updates to users about transaction statuses.
Reduces polling overhead and improves responsiveness.


## Technology Stack

The choice of technologies was made based on performance, scalability, and reliability requirements:
.NET 9 (C#): Provides high performance and strong type safety.
ASP.NET Core: Enables fast and secure API development.
Entity Framework Core: Simplifies database interactions while maintaining efficiency.
MS SQL Server: Ensures data integrity and supports complex transactions.
Apache Kafka: Handles asynchronous processing and event-driven architecture.
Redis: Optimizes performance with low-latency caching.
Docker & Docker Compose: Facilitates containerized deployment and portability.

## Installation and Setup

### 1. Clone the Repository
```sh
# Navigate to your projects directory
mkdir -p ~/projects && cd ~/projects

# Clone the repository
git clone https://github.com/LarecOstrov/BankingSolution.git
cd BankingSolution
```

### 2. Install Docker and Docker Compose
#### Windows & macOS
Follow the official Docker installation guide:
[Docker Installation Guide](https://docs.docker.com/desktop/setup/install/)

#### Linux (Ubuntu)
```sh
sudo apt update
sudo apt install -y docker.io docker-compose
sudo systemctl enable --now docker
sudo usermod -aG docker $USER
```
Verify installation:
```sh
docker --version
docker-compose --version
```
Ensure Docker Compose version is **2.0 or higher**.

### 3. Start the Application
Navigate to the directory containing `docker-compose.yml`:
```sh
cd BankingSolution
```
Build and start the containers:
```sh
docker-compose up --build -d
```
Check running services:
```sh
docker ps -a
```
All services should be running without errors.

### 4. Verify API is Running
Check the health endpoint:
```sh
curl http://localhost:8080/health
```
If successful, the API is running.

```sh
curl http://localhost:8082/health
```
If successful, the Worker is running.

## Usage

### 1. Register a New Account 

The first user must be registered with the administrator role. He will be automatically verified.
Admin roles allow acess to GraphQL and all access to REST API. Create PaymentService role for access to deposit and withdraw operation.
```sh
POST http://localhost:8080/api/auth/register
Content-Type: application/json

{
    "fullName": "Full Name",
    "email": "admin@example.com",
    "password": "Qwerty1!",
    "role": "Admin"
}
```

### 2. Login 

```sh
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{   
    "email": "admin@gmail.com",
    "password": "Qwerty1!"
}
```
Use recived accessToken for header "Authorization": "Bearer {token}"

For refresh token use refreshToken:

```sh
http://localhost:8080/api/auth/refresh
Content-Type: application/json
"33be07b1-6325-4136-a443-84e73e45467a"
```

### 2. QraphQl
```sh
POST http://localhost:8080/graphql
```
Use recived Admin accessToken for header "Authorization": "Bearer {token}"

### 3. Make a Deposit
```sh
POST http://localhost:8080/api/transactions/deposit
Content-Type: application/json

{
    "ToAccountId": "{Guid}",
    "Amount": "1000"
}
```

### 4. Make a Withdraw
```sh
POST http://localhost:8080/api/transactions/withdraw
Content-Type: application/json

{
    "FromAccountId": "{Guid}",
    "Amount": "1000"
}
```

### 5. Transfer Funds
```sh
POST http://localhost:8080/api/transactions/transfer
Content-Type: application/json

{
  "FromAccountId": "{Guid}",
  "ToAccountId": "{Guid}",
  "Amount": "500"
}
```

### 6. WebSockets for Real-time Updates
Connect to the WebSocket endpoint using an access token:
```sh
ws://localhost:8080/ws?access_token={accessToken}
```
This will enable real-time transaction notifications.

## Database Schema
<img width="467" alt="image" src="https://github.com/user-attachments/assets/2f5abbbd-b307-4e69-96fb-396af10cd46a" />


## Running Tests
Automated tests are included in the solution. Run tests using:
```sh
dotnet test
```

## Deployment
Auto-migration for database allow only in Development mode.
For production deployment, update the ASPNETCORE_ENVIRONMENT to Production in docker-compose.yml:
```sh
docker-compose down
docker-compose up --build -d
```

## License
This project is licensed under the MIT License.

## Contact
For any inquiries, reach out via GitHub Issues.

