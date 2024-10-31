build:
	@dotnet build

test:
	@dotnet test

server:
	@docker run \
		--user 1000:1000 \
		-p 8081:8081 \
		-v $(realpath src)/Testcontainers.Spire/conf/server:/etc/spire/server \
		ghcr.io/spiffe/spire-server:1.10.0 \
		-config /etc/spire/server/server.conf

agent:
	@docker run \
		-p 8080:8080 \
		-v $(realpath src)/Testcontainers.Spire/conf/agent:/etc/spire/agent \
		ghcr.io/spiffe/spire-agent:1.10.0 \
		-config /etc/spire/agent/agent.conf

stop:
