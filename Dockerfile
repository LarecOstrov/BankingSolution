FROM mcr.microsoft.com/mssql/server:2022-latest

# Install necessary packages
USER root
RUN apt-get update && apt-get install -y curl apt-transport-https gnupg2 \
    && curl -sSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - \
    && curl -sSL https://packages.microsoft.com/config/ubuntu/20.04/prod.list | tee /etc/apt/sources.list.d/mssql-release.list \
    && apt-get update \
    && apt-get remove -y unixodbc unixodbc-dev libodbc1 odbcinst1debian2 \
    && ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc unixodbc-dev \
    && echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc \
    && apt-get clean

# Change the user to mssql
USER mssql

# Start SQL Server
CMD ["/opt/mssql/bin/sqlservr"]
