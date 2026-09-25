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

# NOTA: las migraciones de EF Core NO se ejecutan en build time.
# Render no expone las variables de entorno del servicio como build args de Docker,
# y buildear contra la BD de produccion es una mala practica (mutacion de datos
# durante el build). Se aplican al arrancar la app en Program.cs
# (Database.MigrateAsync), que es donde existe la connection string.

# =========================================================
# Imagen Final Liviana de Ejecución (ASP.NET 10)
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Variables de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production

# Exponer el puerto que Render asigna (Render inyecta PORT=10000 por defecto)
EXPOSE 8080
EXPOSE 10000

# Comando de inicio: solo la app (migraciones ya se ejecutaron en build)
ENTRYPOINT ["sh", "-c", "dotnet EcommerceApp.dll --urls http://0.0.0.0:${PORT:-8080}"]
