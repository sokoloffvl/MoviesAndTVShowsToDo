# Build React client
FROM node:22-slim AS client-build
WORKDIR /src/client
COPY Source/App/Client/ ./
RUN npm install && npm run build

# Build and publish API (SPA copied into wwwroot by client-build stage)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY Source/App/Api/MoviesAndTVShowsToDo.Api.csproj Source/App/Api/
COPY Source/App/Client/Client.esproj Source/App/Client/
RUN dotnet restore Source/App/Api/MoviesAndTVShowsToDo.Api.csproj
COPY Source/App/Api/ Source/App/Api/
RUN mkdir -p Source/App/Api/wwwroot
COPY --from=client-build /src/client/dist/ Source/App/Api/wwwroot/
RUN dotnet publish Source/App/Api/MoviesAndTVShowsToDo.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:SkipSpaBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=api-build /app/publish .
ENTRYPOINT ["dotnet", "MoviesAndTVShowsToDo.Api.dll"]
