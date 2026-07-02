# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5003


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EventTracker.Contracts/EventTracker.Contracts.csproj", "EventTracker.Contracts/"]
COPY ["EventTracker.BookingsService/EventTracker.BookingsService.Presentation/EventTracker.BookingsService.Presentation.csproj", "EventTracker.BookingsService/EventTracker.BookingsService.Presentation/"]
COPY ["EventTracker.BookingsService/EventTracker.BookingsService.Application/EventTracker.BookingsService.Application.csproj", "EventTracker.BookingsService/EventTracker.BookingsService.Application/"]
COPY ["EventTracker.BookingsService/EventTracker.BookingsService.Domain/EventTracker.BookingsService.Domain.csproj", "EventTracker.BookingsService/EventTracker.BookingsService.Domain/"]
COPY ["EventTracker.BookingsService/EventTracker.BookingsService.Infrastructure/EventTracker.BookingsService.Infrastructure.csproj", "EventTracker.BookingsService/EventTracker.BookingsService.Infrastructure/"]
RUN dotnet restore "EventTracker.BookingsService/EventTracker.BookingsService.Presentation/EventTracker.BookingsService.Presentation.csproj"
COPY . .
WORKDIR "/src/EventTracker.BookingsService/EventTracker.BookingsService.Presentation"
RUN dotnet build "EventTracker.BookingsService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EventTracker.BookingsService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventTracker.BookingsService.Presentation.dll"]
