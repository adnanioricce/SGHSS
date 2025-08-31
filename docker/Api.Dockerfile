FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/SGHSS.Api/SGHSS.Api.fsproj

# RUN dotnet publish src/SGHSS.Api/SGHSS.Api.fsproj -c Release -p:IsTransformWebConfigDisabled=true -r linux-x64 --self-contained true /p:PublishSingleFile=true -o /app
RUN dotnet publish src/SGHSS.Api/SGHSS.Api.fsproj -c Release -p:IsTransformWebConfigDisabled=true -r linux-x64 --self-contained true /p:PublishSingleFile=false -o /app

# FROM mcr.microsoft.com/dotnet/runtime:8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app ./
# COPY /publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "SGHSS.Api.dll"]
# ENTRYPOINT ["./SGHSS.Api"]
# ENTRYPOINT [ "bash" ]
