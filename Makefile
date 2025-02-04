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
	@dotnet pack src/Testcontainers.Spire/Testcontainers.Spire.csproj \
		--configuration Release \
		--output .nupkg \
		-p:IncludeSymbols=true \
		-p:SymbolPackageFormat=snupkg

