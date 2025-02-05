VERSION := 0.0.1

.PHONY: restore
restore:
	@dotnet restore

.PHONY: build
build:
	@dotnet build --no-restore

.PHONY: test
test:
	@dotnet test --no-build --verbosity normal

.PHONY: clean
clean:
	@dotnet clean

.PHONY: pack
pack:
	@rm -rf .nupkg/*
	@dotnet pack src/Spiffe.Testcontainers.Spire/Spiffe.Testcontainers.Spire.csproj \
		--configuration Release \
		--output .nupkg \
		-p:IncludeSymbols=true \
		-p:SymbolPackageFormat=snupkg

.PHONY: push
push:
	@dotnet nuget push nupkg/Spiffe.$(VERSION).nupkg --api-key $(ENV_NUGET_API_KEY) --source https://api.nuget.org/v3/index.json
