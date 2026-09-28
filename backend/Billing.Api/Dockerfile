FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Billing.Domain/Billing.Domain.csproj Billing.Domain/
COPY Billing.Application/Billing.Application.csproj Billing.Application/
COPY Billing.Infrastructure/Billing.Infrastructure.csproj Billing.Infrastructure/
COPY Billing.Api/Billing.Api.csproj Billing.Api/
RUN dotnet restore Billing.Api/Billing.Api.csproj
COPY . .
RUN dotnet publish Billing.Api/Billing.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Billing.Api.dll"]
