FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App

COPY . ./

RUN dotnet restore

RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT [ "dotnet", "SimRacingSchedule.Api.dll" ]

EXPOSE 8080
