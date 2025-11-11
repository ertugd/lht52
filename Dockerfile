# --- Build aşaması ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve bağımlılıkları indir
COPY ["istiklal-karacasu-lorawan.csproj", "."]
RUN dotnet restore "./istiklal-karacasu-lorawan.csproj"

# Geri kalan dosyaları kopyala ve build et
COPY . .
RUN dotnet publish "./istiklal-karacasu-lorawan.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Çalışma aşaması ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Yayınlanan dosyaları kopyala
COPY --from=build /app/publish .

# Render'ın port yönetimi için
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "istiklal-karacasu-lorawan.dll"]
