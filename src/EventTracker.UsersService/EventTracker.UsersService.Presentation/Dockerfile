# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EventTracker.UsersService/EventTracker.UsersService.Presentation/EventTracker.UsersService.Presentation.csproj", "EventTracker.UsersService/EventTracker.UsersService.Presentation/"]
COPY ["EventTracker.UsersService/EventTracker.UsersService.Application/EventTracker.UsersService.Application.csproj", "EventTracker.UsersService/EventTracker.UsersService.Application/"]
COPY ["EventTracker.UsersService/EventTracker.UsersService.Domain/EventTracker.UsersService.Domain.csproj", "EventTracker.UsersService/EventTracker.UsersService.Domain/"]
COPY ["EventTracker.UsersService/EventTracker.UsersService.Infrastructure/EventTracker.UsersService.Infrastructure.csproj", "EventTracker.UsersService/EventTracker.UsersService.Infrastructure/"]
RUN dotnet restore "EventTracker.UsersService/EventTracker.UsersService.Presentation/EventTracker.UsersService.Presentation.csproj"
COPY . .
WORKDIR "/src/EventTracker.UsersService/EventTracker.UsersService.Presentation"
RUN dotnet build "EventTracker.UsersService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EventTracker.UsersService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventTracker.UsersService.Presentation.dll"]
