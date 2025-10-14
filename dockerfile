# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy sln & csproj first for layer caching
COPY MicroserviceTest.sln .
COPY MicroserviceTest.csproj ./ 
RUN dotnet restore

# copy the rest
COPY . .
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /out --no-build

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

# Kestrel on 8080 like you already do
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# basic healthcheck
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "MicroserviceTest.dll"]
