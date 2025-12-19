FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["T6.TableService/T6.TableService.csproj", "T6.TableService/"]
COPY ["DiningPhilosophers.Contracts/DiningPhilosophers.Contracts.csproj", "DiningPhilosophers.Contracts/"]
RUN dotnet restore "T6.TableService/T6.TableService.csproj"
COPY . .
WORKDIR "/src/T6.TableService"
RUN dotnet build "T6.TableService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "T6.TableService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "T6.TableService.dll"]

