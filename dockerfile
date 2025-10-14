# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project and solution files first for layer caching.
COPY "MicroserviceTest.sln" .
COPY "MicroserviceTest.csproj" .
COPY "MicroserviceTest.Tests/MicroserviceTest.Tests.csproj" "MicroserviceTest.Tests/"

# Restore dependencies for the entire solution
RUN dotnet restore "MicroserviceTest.sln"

# Copy the rest of the source code
COPY . .

# Build and publish only the main application project for the final image
RUN dotnet build "MicroserviceTest.csproj" -c Release --no-restore
RUN dotnet publish "MicroserviceTest.csproj" -c Release -o /out --no-build

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .

# Kestrel on 8080 like you already do
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# basic healthcheck
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "MicroserviceTest.dll"]
