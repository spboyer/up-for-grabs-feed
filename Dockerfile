FROM microsoft/dotnet:2.1.6-runtime-alpine AS base
WORKDIR /app
EXPOSE 80
# install subversion for alpine
RUN apk --no-cache add subversion
# download the data for projects
RUN svn export https://github.com/up-for-grabs/up-for-grabs.net/trunk/_data/projects data

FROM microsoft/dotnet:2.1.500-sdk-alpine AS build
WORKDIR /src
COPY ["up-for-grabs-feed.csproj", "./"]
RUN dotnet restore "./up-for-grabs-feed.csproj"
COPY . .
WORKDIR /src/.
RUN dotnet build "up-for-grabs-feed.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "up-for-grabs-feed.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "up-for-grabs-feed.dll"]
