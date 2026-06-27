# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ATG.Platform.Domain/ATG.Platform.Domain.csproj ATG.Platform.Domain/
COPY ATG.Platform.Application/ATG.Platform.Application.csproj ATG.Platform.Application/
COPY ATG.Platform.Infrastructure/ATG.Platform.Infrastructure.csproj ATG.Platform.Infrastructure/
COPY ATG.Platform.API/ATG.Platform.API.csproj ATG.Platform.API/
RUN dotnet restore ATG.Platform.API/ATG.Platform.API.csproj

COPY ATG.Platform.Domain/ ATG.Platform.Domain/
COPY ATG.Platform.Application/ ATG.Platform.Application/
COPY ATG.Platform.Infrastructure/ ATG.Platform.Infrastructure/
COPY ATG.Platform.API/ ATG.Platform.API/

RUN dotnet publish ATG.Platform.API/ATG.Platform.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN mkdir -p /app/uploads

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ATG.Platform.API.dll"]
