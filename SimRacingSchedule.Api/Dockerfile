FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SimRacingSchedule.Api/SimRacingSchedule.Api.csproj",                                   "SimRacingSchedule.Api/"]
COPY ["SimRacingSchedule.Application/SimRacingSchedule.Application.csproj",                   "SimRacingSchedule.Application/"]
COPY ["SimRacingSchedule.Application.Telegram/SimRacingSchedule.Application.Telegram.csproj", "SimRacingSchedule.Application.Telegram/"]
COPY ["SimRacingSchedule.Core/SimRacingSchedule.Core.csproj",                                 "SimRacingSchedule.Core/"]
COPY ["SimRacingSchedule.Infrastructure/SimRacingSchedule.Infrastructure.csproj",             "SimRacingSchedule.Infrastructure/"]

RUN dotnet restore "SimRacingSchedule.Api/SimRacingSchedule.Api.csproj"

# DebugType=none  — исключает .pdb-файлы - символы отладки - из артефактов публикации.
# DebugSymbols=false — отключает генерацию отладочных символов при компиляции.
COPY . .
RUN dotnet publish "SimRacingSchedule.Api/SimRacingSchedule.Api.csproj" \
    -c Release \
    --no-restore \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /App
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "SimRacingSchedule.Api.dll"]
