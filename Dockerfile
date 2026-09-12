FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["GeniusVendas.Api.csproj", "./"]
RUN dotnet restore "GeniusVendas.Api.csproj"
COPY . .
RUN dotnet publish "GeniusVendas.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000}
EXPOSE 10000
ENTRYPOINT ["dotnet", "GeniusVendas.Api.dll"]
