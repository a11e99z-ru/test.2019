using Grpc.Core;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;
using Microservices.Shared.Grpc;
using Microservices.Shared.Helpers;
using Serilog;

namespace Microservices.Gateway.Infrastructure.KlineUtils;

public sealed class KlineGrpcService
    : MarketDataService.MarketDataServiceBase
{
    private readonly IDataMediator _mediator;
    private readonly Serilog.ILogger _logger = Log.ForContext<KlineGrpcService>();

    public KlineGrpcService(IDataMediator mediator)
        => _mediator = mediator;
    
    public override async Task SubscribeKlines(KlinesSubscriptionRequest request, IServerStreamWriter<MarketDataUpdate> respStream, ServerCallContext ctx)
    {
        var symbols = request.Symbols.Select(x => Str16.FromAscii(x)).ToHashSet();
        var period = ((int)request.Period).ToKlinePeriod();
        _logger.Information("KlineGrpcService.SubscribeKlines({per}): {syms}", period,
            string.Join(",", request.Symbols.OrderBy(x => x)));
        var klines = new KlinesGenerator(period);

        TaskCompletionSource<string> newError = new();
        TaskCompletionSource<TradeInfo> newTrade = new();

        _mediator.OnError += OnError;
        using var unsubError = Defer.It(_mediator, it => it.OnError -= OnError);
        _mediator.OnTrade += OnTrade;
        using var unsubTrade = Defer.It(_mediator, it => it.OnTrade -= OnTrade);

        var ts = new TradeInfo[1];
        var ct = ctx.CancellationToken;
        try
        {
            var md = new MarketDataUpdate();
            while (!ct.IsCancellationRequested)
            {
                var fin = Task.WhenAny(newError.Task, newTrade.Task).WaitAsync(ct);
                if (fin.Result is Task<string> err)
                {
                    md.Error = new() { Message = err.Result };
                }
                else if (fin.Result is Task<TradeInfo> trade)
                {
                    ts[0] = trade.Result;
                    if (!symbols.Contains(ts[0].Info.Symbol))
                        continue;
                    var bars = klines.Update(ts);
                    if (bars.Count == 0 || bars[0].Kline.Volume == 0)
                        continue;
                    ref var info = ref bars.Array![0].Info;
                    ref var kline = ref bars.Array![0].Kline;
                    md.Kline = new()
                    {
                        Period = request.Period,
                        Info = new()
                        {
                            Symbol = info.Symbol.ToString(),
                            Timestamp = info.Timestamp
                        },
                        Kline = new()
                        {
                            Open = kline.Open,
                            High = kline.High,
                            Low = kline.Low,
                            Close = kline.Close,
                            Volume = kline.Volume,
                            Value = kline.Value
                        }
                    };
                }
                await respStream.WriteAsync(md); //, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in KlineGrpcService.SubscribeKlines");
        }

        void OnError(string value)
        {
            var hndl = Interlocked.Exchange(ref newError, new());
            hndl.SetResult(value);
        }
        void OnTrade(TradeInfo value)
        {
            var hndl = Interlocked.Exchange(ref newTrade, new());
            hndl.SetResult(value);
        }
    }
}
