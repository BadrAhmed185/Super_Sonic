# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy sln and csproj first for better layer caching
COPY Super_Sonic.sln ./ 
COPY Super_Sonic/Super_Sonic.csproj Super_Sonic/
RUN dotnet restore Super_Sonic/Super_Sonic.csproj

# copy remaining files and publish
COPY . .
RUN dotnet publish Super_Sonic/Super_Sonic.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Super_Sonic.dll"]
