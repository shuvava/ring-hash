FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 50533
EXPOSE 44322

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["examples/producer/producer.csproj", "examples/producer/"]
RUN dotnet restore "examples/producer/producer.csproj"
COPY . .
WORKDIR "/src/examples/producer"
RUN dotnet build "producer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "producer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "producer.dll"]