FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["JumpServer/JumpServer.csproj", "JumpServer/"]
RUN dotnet restore "JumpServer/JumpServer.csproj"
COPY . .
WORKDIR "/src/JumpServer"
RUN dotnet build "JumpServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JumpServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JumpServer.dll"]
