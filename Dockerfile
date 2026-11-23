# Runtime base image. CI passes the same tag pinned to a digest (RUNTIME_IMAGE=...@sha256:...) so the image's
# org.opencontainers.image.base.digest label records exactly what it was built from.
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /App

COPY . ./
ARG TARGETARCH
RUN dotnet restore -a $TARGETARCH
RUN dotnet publish -a $TARGETARCH -c Release -o out

# Chiseled: no shell or package manager, non-root by default. No ICU/tzdata, hence InvariantGlobalization in the csproj.
FROM ${RUNTIME_IMAGE}
WORKDIR /App
COPY --from=build-env /App/out .
EXPOSE 8080
# The chiseled image already defaults to this non-root user; kept explicit.
USER $APP_UID
CMD ["./LubeLogMCP"]
