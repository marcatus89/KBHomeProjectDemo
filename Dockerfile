# Dockerfile (multi-stage) cho .NET 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore
COPY ["DoAnTotNghiep.csproj", "./"]
RUN dotnet restore "./DoAnTotNghiep.csproj"

# Copy toàn bộ project và publish
COPY . .
RUN dotnet publish "DoAnTotNghiep.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DoAnTotNghiep.dll"]
