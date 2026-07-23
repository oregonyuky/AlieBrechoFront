FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/AlieBrecho.Domain/AlieBrecho.Domain.csproj src/AlieBrecho.Domain/
COPY src/AlieBrecho.Application/AlieBrecho.Application.csproj src/AlieBrecho.Application/
COPY src/AlieBrecho.Infrastructure/AlieBrecho.Infrastructure.csproj src/AlieBrecho.Infrastructure/
COPY src/AlieBrecho.Presentation/AlieBrecho.Presentation.csproj src/AlieBrecho.Presentation/
RUN dotnet restore src/AlieBrecho.Presentation/AlieBrecho.Presentation.csproj

COPY src/ src/
RUN dotnet publish src/AlieBrecho.Presentation/AlieBrecho.Presentation.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/App_Data/DataProtectionKeys \
    && chown -R "$APP_UID:$APP_UID" /app/App_Data

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "AlieBrecho.Presentation.dll"]
