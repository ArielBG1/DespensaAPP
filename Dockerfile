# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DespensaApp.csproj ./
RUN dotnet restore DespensaApp.csproj

COPY . .
RUN dotnet publish DespensaApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Render inyecta la variable PORT; 8080 es el valor por defecto para pruebas locales.
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DespensaApp.dll"]
