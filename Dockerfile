# =========================================================
# Etapa 1: Compilación y Publicación (SDK .NET 10)
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias primero
COPY ["EcommerceApp.csproj", "./"]
RUN dotnet restore "EcommerceApp.csproj"

# Copiar el resto del código y compilar la versión optimizada
COPY . .
RUN dotnet publish "EcommerceApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================================
# Etapa 2: Imagen Final Liviana de Ejecución (ASP.NET 10)
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Variables de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicio
ENTRYPOINT ["dotnet", "EcommerceApp.dll"]
