FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /repo

COPY ["src/SynBot/SynBot.csproj", "src/SynBot/"]

RUN dotnet restore "src/SynBot/SynBot.csproj"

COPY . .

RUN dotnet publish "src/SynBot/SynBot.csproj" -c Release -o publish -p UseAppHost=false


FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS runtime

RUN apk add --no-cache tzdata

ENV TZ="Asia/Shanghai"

COPY --from=build /repo/publish /app

WORKDIR /data

ENTRYPOINT ["dotnet", "/app/SynBot.dll"]