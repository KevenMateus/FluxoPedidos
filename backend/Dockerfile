# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore com cache: copia só os .csproj e a solution primeiro.
COPY Pedidos.sln ./
COPY Pedidos.Domain/Pedidos.Domain.csproj Pedidos.Domain/
COPY Pedidos.Application/Pedidos.Application.csproj Pedidos.Application/
COPY Pedidos.Infrastructure/Pedidos.Infrastructure.csproj Pedidos.Infrastructure/
COPY Pedidos.Api/Pedidos.Api.csproj Pedidos.Api/
COPY Pedidos.Tests/Pedidos.Tests.csproj Pedidos.Tests/
RUN dotnet restore Pedidos.Api/Pedidos.Api.csproj

# Copia o resto e publica.
COPY . .
RUN dotnet publish Pedidos.Api/Pedidos.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Pedidos.Api.dll"]
