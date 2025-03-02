FROM mcr.microsoft.com/dotnet/sdk:9.0
RUN mkdir src
COPY Program.cs src
COPY Spiffe.Testcontainers.Spire.Tests.Workload.csproj src
COPY Directory.Packages.props src
WORKDIR src
ENTRYPOINT ["dotnet", "run"]
