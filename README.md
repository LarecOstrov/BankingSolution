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

## Technology Stack
- **Backend:** .NET 9 (C#), ASP.NET Core, Entity Framework Core
- **Database:** Microsoft SQL Server
- **Messaging:** Apache Kafka
- **Caching:** Redis
- **Real-time communication:** WebSockets
- **Containerization:** Docker, Docker Compose

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

