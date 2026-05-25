FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["istiklal-karacasu-lorawan.csproj", "."]
RUN dotnet restore "./istiklal-karacasu-lorawan.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "./istiklal-karacasu-lorawan.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Port mapping for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "istiklal-karacasu-lorawan.dll"]
