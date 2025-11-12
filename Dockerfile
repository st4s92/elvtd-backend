# ================================
# STAGE 1: Build the app
# ================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything and publish
COPY . .
RUN dotnet restore ./backend.csproj
RUN dotnet publish ./backend.csproj -c Release -o /app --no-restore

# ================================
# STAGE 2: Runtime environment
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy build output from previous stage
COPY --from=build /app .

# Set ASP.NET to listen on port 5021
ENV ASPNETCORE_URLS=http://+:5021
EXPOSE 5021

# Start the app
ENTRYPOINT ["dotnet", "backend.dll"]

