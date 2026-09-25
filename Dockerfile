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
# Etapa 2: Ejecutar migraciones (usando SDK)
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrate
WORKDIR /app
COPY --from=build /app/publish .

# Instalar EF Core tools
RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# Ejecutar migraciones (necesita connection string)
ARG CONNECTION_STRING
ENV ConnectionStrings__DefaultConnection=${CONNECTION_STRING}
RUN dotnet ef database update

# =========================================================
# Etapa 3: Imagen Final Liviana de Ejecución (ASP.NET 10)
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=migrate /app .

# Variables de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production

# Exponer el puerto que Render asigna (Render inyecta PORT=10000 por defecto)
EXPOSE 8080
EXPOSE 10000

# Comando de inicio: solo la app (migraciones ya se ejecutaron en build)
ENTRYPOINT ["sh", "-c", "dotnet EcommerceApp.dll --urls http://0.0.0.0:${PORT:-8080}"]
