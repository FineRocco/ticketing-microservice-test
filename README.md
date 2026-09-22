# Ticketing Microservice Test

> ⚠️ **Note:** This project is currently under construction. It serves as a practical sandbox to experiment with and demonstrate various modern software engineering, DevOps, and cloud-native observability technologies.

## 📖 Overview
This repository contains a sample microservice application built to practice and integrate modern DevOps, containerization, and advanced observability patterns. Despite its parent folder name suggesting a Spring Boot setup, the application itself is developed using **.NET 9.0**, fully instrumented with **OpenTelemetry**, and configured to be deployed within a **Kubernetes** environment alongside a comprehensive monitoring stack.

## 🚀 Technologies Used

### Application & Framework
* **.NET 9.0 (C#)** - Core framework for building the Web API.
* **ASP.NET Core HealthChecks** - To provide a built-in health endpoint (`/health`) for Kubernetes liveness/readiness probes.

### Observability & Telemetry (LGTM Stack)
* **OpenTelemetry (OTel)** - Unified standard used to capture distributed traces, logs, and metrics.
* **Prometheus** - Pull-based metric aggregation and monitoring.
* **Grafana** - Visualizing the telemetry data via custom dashboards.
* **Loki & Promtail** - Log aggregation and collection.
* **Tempo** - Distributed tracing backend.
* **Alertmanager** - Handling alerts and routing them appropriately.

### Containerization & Orchestration
* **Docker** - Utilizing multi-stage Dockerfiles to optimize image sizes and cache dependency layers.
* **Kubernetes (K8s)** - Orchestration of the microservice and its monitoring stack. Separate configurations are maintained for `prod`, `staging`, and `monitoring` namespaces.

### CI/CD
* **GitHub Actions** - Automated CI/CD pipelines that handle:
  * Unit testing.
  * Building and tagging the Docker image with unique Git SHAs.
  * Pushing to Docker Hub.
  * Automatically deploying the latest image to the Kubernetes production cluster.

## 🧠 Project Logic & Architecture

1. **The Microservice (`Program.cs`)**:
   * The API acts as a simple test bed for OpenTelemetry instrumentation.
   * It exposes a `/hello` endpoint that manually creates an OpenTelemetry span (`hello-work`) and injects custom attributes/tags.
   * Telemetry (logs, traces, metrics) is configured centrally using OpenTelemetry SDKs and routed to an OpenTelemetry Collector via `OtlpExportProtocol.HttpProtobuf`.
   * It directly exposes a `/metrics` endpoint for direct scraping if required.

2. **The Monitoring Stack (`k8s/monitoring`)**:
   * A full monitoring infrastructure is defined as Kubernetes manifests.
   * An **OpenTelemetry Collector** receives the data from the .NET microservice and dispatches it:
     * **Metrics** go to Prometheus.
     * **Traces** go to Tempo.
     * **Logs** go to Loki (additionally, Promtail runs as a DaemonSet to collect pod logs directly).

3. **CI/CD Pipeline (`.github/workflows`)**:
   * On every push to the `main` branch, the pipeline spins up an Ubuntu runner.
   * It restores dependencies, runs unit tests (`dotnet test`), and if successful, initiates the Docker build.
   * The newly created image is pushed to Docker Hub with a specific tag (the short Git commit hash).
   * Finally, it deploys this new image into the Kubernetes production environment (`microservicetest-deployment` namespace) by using `kubectl set image`, followed by a rollout status check to guarantee a successful zero-downtime deployment.

## 🏃 Getting Started (Local Development)

Since the project is built around containerization and K8s, the standard way to run this locally would involve:

1. Running a local Kubernetes cluster (like Minikube, kind, or Docker Desktop K8s).
2. Applying the manifests in the `k8s/monitoring/` directory to spin up the OTel collector, Grafana, Loki, Tempo, and Prometheus.
3. Applying the application's deployment and service manifests from `k8s/staging/` or `k8s/prod/`.

*For basic local execution outside of Docker:*
```bash
cd ticketing-microservice-test
dotnet restore
dotnet run
```
