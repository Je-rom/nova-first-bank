FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Trust the Netskope TLS inspection certificate chain
COPY ["netskope-root-ca.crt", "/usr/local/share/ca-certificates/netskope-root-ca.crt"]
COPY ["netskope-ca.crt", "/usr/local/share/ca-certificates/netskope-ca.crt"]

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && update-ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY ["NovaWallet.csproj", "./"]
RUN dotnet restore "NovaWallet.csproj"

COPY . .
RUN dotnet publish "NovaWallet.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "NovaWallet.dll"]