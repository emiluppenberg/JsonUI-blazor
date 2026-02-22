FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["JsonUI-blazor.csproj", "./"]
COPY ["JsonUI-blazor.Client/JsonUI-blazor.Client.csproj", "JsonUI-blazor.Client/"]
RUN dotnet restore "./JsonUI-blazor.csproj"

COPY . .
RUN dotnet publish "./JsonUI-blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "JsonUI-blazor.dll"]
