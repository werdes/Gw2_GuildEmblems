#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Gw2_GuildEmblem_Cdn.Core/Gw2_GuildEmblem_Cdn.Core.csproj", "Gw2_GuildEmblem_Cdn.Core/"]
RUN dotnet restore "Gw2_GuildEmblem_Cdn.Core/Gw2_GuildEmblem_Cdn.Core.csproj"
COPY . .
WORKDIR "/src/Gw2_GuildEmblem_Cdn.Core"
RUN dotnet build "Gw2_GuildEmblem_Cdn.Core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gw2_GuildEmblem_Cdn.Core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gw2_GuildEmblem_Cdn.Core.dll"]