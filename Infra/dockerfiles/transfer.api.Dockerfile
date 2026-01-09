FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ./BankMore.Transfer.Api/BankMore.Transfer.Api.csproj ./BankMore.Transfer.Api/
RUN dotnet restore ./BankMore.Transfer.Api/BankMore.Transfer.Api.csproj

COPY . .
WORKDIR /src/BankMore.Transfer.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "BankMore.Transfer.Api.dll"]