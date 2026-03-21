# Microservices.Showcase

A project demonstrating how services work in a bloody Enterprise.
Consists of three parts (projects, not counting tests and benchmarks):
- **Microservices.Shared**/<ins>MSh</ins> - an assembly with common code used by other parts.
- **Microservices.Gateway**/<ins>MGw</ins> - a console application that receives market data for several instruments from Bybit via a websocket and distributes it across various channels:
  * <ins>*Trade*</ins> in **Postgres**.
  * <ins>*Levels10*</ins> in **Kafka**.
  * <ins>*OHLC*</ins> (Open, High, Low, Close) candles in **gRPC**.
- **Microservices.Presenter**/<ins>MPr</ins> - **Avalonia** application that receives and displays data from MGw, as well as logs and counters.

## Services involved
- **Consul** for searching <ins>MGw</ins> service in <ins>MPr</ins>.
- **Kafka** for transmitting Levels10 - an order book storing price levels and volumes.
- **PostgreSQL** for storing trades and notifying <ins>MPr</ins> of new trades.
- **gRPC+Protobuf** for communication between <ins>MPr</ins> and <ins>MGw</ins>.
- **Serilog+Loki** for transmitting structured logs to **Grafana**.
- **Prometheus** for transmitting counters.
- **Docker** for running services and **testcontainers**.
- **Grafana** for dashboards (currently, the idea is to embed a browser in <ins>MPr</ins>).
