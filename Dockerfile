FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/PlataformaIncidencias/PlataformaIncidencias.csproj", "src/PlataformaIncidencias/"]
RUN dotnet restore "src/PlataformaIncidencias/PlataformaIncidencias.csproj"
COPY src/PlataformaIncidencias/ src/PlataformaIncidencias/
RUN dotnet publish "src/PlataformaIncidencias/PlataformaIncidencias.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlataformaIncidencias.dll"]
