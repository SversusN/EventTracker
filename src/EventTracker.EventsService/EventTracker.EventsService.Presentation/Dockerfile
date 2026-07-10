# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5002


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EventTracker.Contracts/EventTracker.Contracts.csproj", "EventTracker.Contracts/"]
COPY ["EventTracker.EventsService/EventTracker.EventsService.Presentation/EventTracker.EventsService.Presentation.csproj", "EventTracker.EventsService/EventTracker.EventsService.Presentation/"]
COPY ["EventTracker.EventsService/EventTracker.EventsService.Application/EventTracker.EventsService.Application.csproj", "EventTracker.EventsService/EventTracker.EventsService.Application/"]
COPY ["EventTracker.EventsService/EventTracker.EventsService.Domain/EventTracker.EventsService.Domain.csproj", "EventTracker.EventsService/EventTracker.EventsService.Domain/"]
COPY ["EventTracker.EventsService/EventTracker.EventsService.Infrastructure/EventTracker.EventsService.Infrastructure.csproj", "EventTracker.EventsService/EventTracker.EventsService.Infrastructure/"]
RUN dotnet restore "EventTracker.EventsService/EventTracker.EventsService.Presentation/EventTracker.EventsService.Presentation.csproj"
COPY . .
WORKDIR "/src/EventTracker.EventsService/EventTracker.EventsService.Presentation"
RUN dotnet build "EventTracker.EventsService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EventTracker.EventsService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventTracker.EventsService.Presentation.dll"]
