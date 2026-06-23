FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Install Node.js (required for webpack build of frontend assets)
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y --no-install-recommends nodejs && \
    apt-get clean && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY ["src/ChessTrainerApp/ChessTrainerApp.csproj", "src/ChessTrainerApp/"]
RUN dotnet restore "src/ChessTrainerApp/ChessTrainerApp.csproj"
COPY . .
WORKDIR "/src/src/ChessTrainerApp"
RUN dotnet build "ChessTrainerApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChessTrainerApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MjrChess.Trainer.dll"]