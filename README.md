# Microservices.Showcase

A project demonstrating how services work in a Bloody Enterprise.
<br>Consists of three parts (projects, not counting tests and benchmarks):
- **Microservices.Shared**/<ins>MSh</ins> - an assembly with common code used by other parts.
- **Microservices.Gateway**/<ins>MGw</ins> - a console application that receives market data for several instruments from Bybit via a websocket and distributes it across various channels:
  * <ins>*Trade*</ins> in **Postgres**.
  * <ins>*Levels10*</ins> in **Kafka**.
  * <ins>*OHLC*</ins> (Open, High, Low, Close) candles in **gRPC**.
- **Microservices.Presenter**/<ins>MPr</ins> - **Avalonia** application that receives and displays data from MGw, as well as logs and counters.

## Services involved
- **Consul** for searching <ins>MGw</ins> service in <ins>MPr</ins>.
- **Bybit API + WebSockets + JSON** - market-data for BTCUSDT, ETHUSDT in <ins>MGw</ins>.
- **Kafka** for transmitting Levels10 - an order book storing price levels and volumes.
- **PostgreSQL** for storing trades and notifying <ins>MPr</ins> of new trades through table-triggers + DB-notification mechanism.
- **gRPC+Protobuf** for communication between <ins>MPr</ins> and <ins>MGw</ins>.
- **Serilog+Loki** for transmitting structured logs to **Grafana**.
- **Prometheus** for transmitting metrics to **Grafana**.
- **Docker** for running services and **testcontainers**.
- **Grafana** for dashboards (currently, the idea is to embed a browser in <ins>MPr</ins>).

## TODO:
- ~~Domain entities: Trades, Levels, OHLC etc~~
- ~~Kafka reader & writer~~
- ~~DB (**PostgreSQL**) notification, writer & reader~~
- ~~Bybit' WebSocket subscriber & listener~~
- add **Prometheius** metrics to <ins>MGw</ins>
- add **Protobuf**' <ins>proto</ins> & **gRPC** server to <ins>MGw</ins> and register it in **Consul**
- add **Loki** logs to services (50%)
- set up **Grafana + Loki + Prometheus** server for logs and metrics (0%)
- **Avalonia** Client App <ins>MPr</ins> to receive market-data from <ins>MGw</ins> and show it to user (0%)
- more integrations tests with testcontainers (30%)

### Under active (re)construction: 90% done, 90% left.
