# Use .NET SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory in container
WORKDIR /app

# Copy all files into container
COPY . .

# Build to create publish file
RUN dotnet publish -c Release -o out

# ------------------------------------------------

# Use .NET Runtime to run app (smaller than SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set working directory
WORKDIR /app

# Copy result from build stage
COPY --from=build /app/out .

# Open port
EXPOSE 8080

# Run app
ENTRYPOINT [ "dotnet", "TodoApi.dll" ]