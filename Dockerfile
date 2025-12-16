FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
WORKDIR /src

COPY ["IdentityService.sln", "."]
COPY ["src/IdentityService.Core/IdentityService.Core.csproj", "src/IdentityService.Core/"]
COPY ["src/IdentityService.Application/IdentityService.Application.csproj", "src/IdentityService.Application/"]
COPY ["src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/IdentityService.Infrastructure/"]
COPY ["src/IdentityService.Api/IdentityService.Api.csproj", "src/IdentityService.Api/"]

RUN dotnet restore "IdentityService.sln"

COPY . .

RUN dotnet build "IdentityService.sln" -c Release -o /app/build

FROM build as publish
RUN dotnet publish "src/IdentityService.Api/IdentityService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80 443

ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
