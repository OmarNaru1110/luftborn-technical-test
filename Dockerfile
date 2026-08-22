FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DATA/DATA.csproj DATA/
COPY CORE/CORE.csproj CORE/
COPY API/API/API.csproj API/API/
RUN dotnet restore API/API/API.csproj

COPY DATA/ DATA/
COPY CORE/ CORE/
COPY API/API/ API/API/
RUN dotnet publish API/API/API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]
