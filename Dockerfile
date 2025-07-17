# Use .NET 8.0 SDK for build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and restore dependencies
COPY . . 
RUN dotnet restore Super_Sonic.sln

# Build and publish
RUN dotnet publish Super_Sonic/Super_Sonic.csproj -c Release -o /out

# Use .NET 8.0 ASP.NET Runtime for runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "Super_Sonic.dll"]
