# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy sln and csproj files first (better layer caching)
COPY DevOpsGuard.sln ./
COPY src/DevOpsGuard.Api/DevOpsGuard.Api.csproj src/DevOpsGuard.Api/
COPY src/DevOpsGuard.Application/DevOpsGuard.Application.csproj src/DevOpsGuard.Application/
COPY src/DevOpsGuard.Domain/DevOpsGuard.Domain.csproj src/DevOpsGuard.Domain/
COPY src/DevOpsGuard.Infrastructure/DevOpsGuard.Infrastructure.csproj src/DevOpsGuard.Infrastructure/

RUN dotnet restore DevOpsGuard.sln

# now copy the rest
COPY . .

# publish
RUN dotnet publish src/DevOpsGuard.Api/DevOpsGuard.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# listen on 8080 in container
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish ./
EXPOSE 8080

ENTRYPOINT ["dotnet", "DevOpsGuard.Api.dll"]
