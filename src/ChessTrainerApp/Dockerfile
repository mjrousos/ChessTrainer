FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
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
ENTRYPOINT ["dotnet", "ChessTrainerApp.dll"]