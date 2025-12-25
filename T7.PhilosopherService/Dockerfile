FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["T7.PhilosopherService/T7.PhilosopherService.csproj", "T7.PhilosopherService/"]
COPY ["DiningPhilosophers.Contracts/DiningPhilosophers.Contracts.csproj", "DiningPhilosophers.Contracts/"]
RUN dotnet restore "T7.PhilosopherService/T7.PhilosopherService.csproj"
COPY . .
WORKDIR "/src/T7.PhilosopherService"
RUN dotnet build "T7.PhilosopherService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "T7.PhilosopherService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "T7.PhilosopherService.dll"]

