FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY Shared/   Shared/
COPY Server/   Server/

RUN dotnet publish Server/KspConnected.Server.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -p:DebugType=none \
    -o /app/out

# ── runtime image ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app

COPY --from=build /app/out .

# Relay mode is on by default when running via Docker so no port forwarding
# is required on the host side — just expose port 7654 and share the address.
ENV PORT=7654

# Write a minimal server.json enabling relay mode
RUN echo '{"Port":7654,"MaxPlayers":64,"ServerName":"KSP-Connected Relay","WelcomeMessage":"Welcome!","RelayMode":true}' \
    > /app/server.json

EXPOSE 7654
ENTRYPOINT ["dotnet", "KspConnected.Server.dll", "--relay"]
