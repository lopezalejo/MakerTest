FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SolidarityGrid.sln ./
COPY MakerTest/SolidarityGrid.Domain/SolidarityGrid.Domain.csproj src/SolidarityGrid.Domain/
COPY MakerTest/SolidarityGrid.Application/SolidarityGrid.Application.csproj src/SolidarityGrid.Application/
COPY MakerTest/SolidarityGrid.Infrastructure/SolidarityGrid.Infrastructure.csproj src/SolidarityGrid.Infrastructure/
COPY MakerTest/SolidarityGrid.Api/SolidarityGrid.Api.csproj src/SolidarityGrid.Api/
RUN dotnet restore src/SolidarityGrid.Api/SolidarityGrid.Api.csproj
COPY MakerTest/ src/
RUN dotnet publish src/SolidarityGrid.Api/SolidarityGrid.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV TZ=America/Bogota
ENV LANG=es_CO.UTF-8
ENV LC_ALL=es_CO.UTF-8
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata locales \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime \
    && echo $TZ > /etc/timezone \
    && sed -i '/es_CO.UTF-8/s/^# //g' /etc/locale.gen \
    && locale-gen es_CO.UTF-8 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "SolidarityGrid.Api.dll"]