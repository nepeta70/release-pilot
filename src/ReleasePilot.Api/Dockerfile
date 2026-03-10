# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# The paths here remain relative to the ROOT of the repo (the build context)
COPY *.slnx ./
COPY src/ReleasePilot.Api/*.csproj src/ReleasePilot.Api/
COPY src/ReleasePilot.Application/*.csproj src/ReleasePilot.Application/
COPY src/ReleasePilot.Domain/*.csproj src/ReleasePilot.Domain/
COPY src/ReleasePilot.Infrastructure/*.csproj src/ReleasePilot.Infrastructure/
COPY tests/ReleasePilot.Domain.Tests/*.csproj tests/ReleasePilot.Domain.Tests/

RUN dotnet restore

# Copy the source code
COPY . .

WORKDIR /app/src/ReleasePilot.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "ReleasePilot.Api.dll"]