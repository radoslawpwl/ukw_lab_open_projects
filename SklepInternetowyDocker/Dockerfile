FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SklepInternetowyDocker.csproj", "./"]
RUN dotnet restore "SklepInternetowyDocker.csproj"

COPY . .
RUN dotnet publish "SklepInternetowyDocker.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SklepInternetowyDocker.dll"]