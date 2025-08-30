FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src

COPY . .
RUN dotnet restore

RUN dotnet publish -c Release -p:IsTransformWebConfigDisabled=true -o /app

# FROM mcr.microsoft.com/dotnet/runtime:8.0
# FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
WORKDIR /app

# COPY --from=build /app ./
# COPY /publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "SGHSS.Api.dll"]
# ENTRYPOINT [ "bash" ]
