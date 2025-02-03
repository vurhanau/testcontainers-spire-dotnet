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
