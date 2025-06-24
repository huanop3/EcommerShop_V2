# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers to leverage caching
COPY ["GateWayService/GateWayService.csproj", "GateWayService/"]
RUN dotnet restore "GateWayService/GateWayService.csproj"

# Copy the rest of the application's code
COPY ["GateWayService/", "GateWayService/"]
WORKDIR "/src/GateWayService"
RUN dotnet publish "GateWayService.csproj" -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GateWayService.dll"]