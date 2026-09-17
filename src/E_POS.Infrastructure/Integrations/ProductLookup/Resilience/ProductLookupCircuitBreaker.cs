using E_POS.Application.Common.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Integrations.ProductLookup.Resilience;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen,
}

public interface IProductLookupCircuitBreakerRegistry
{
    IProductLookupCircuitBreaker GetOrCreate(string providerName);
}

public interface IProductLookupCircuitBreaker
{
    CircuitState State { get; }
    bool TryExecute(out string? rejectionReason);
    void RecordSuccess();
    void RecordFailure();
    void Reset();
}

public sealed class ProductLookupCircuitBreakerRegistry : IProductLookupCircuitBreakerRegistry
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IProductLookupCircuitBreaker> _breakers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Microsoft.Extensions.Options.IOptions<E_POS.Application.Modules.Tenant.CatalogProduct.Options.ExternalProductLookupOptions> _options;
    private readonly Microsoft.Extensions.Logging.ILogger<ProductLookupCircuitBreaker> _logger;

    public ProductLookupCircuitBreakerRegistry(
        IDateTimeProvider dateTimeProvider,
        Microsoft.Extensions.Options.IOptions<E_POS.Application.Modules.Tenant.CatalogProduct.Options.ExternalProductLookupOptions> options,
        Microsoft.Extensions.Logging.ILogger<ProductLookupCircuitBreaker> logger)
    {
        _dateTimeProvider = dateTimeProvider;
        _options = options;
        _logger = logger;
    }

    public IProductLookupCircuitBreaker GetOrCreate(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        return _breakers.GetOrAdd(providerName.Trim(), name =>
            new ProductLookupCircuitBreaker(
                name,
                _options.Value.Resilience.CircuitBreaker.FailureThreshold,
                TimeSpan.FromSeconds(_options.Value.Resilience.CircuitBreaker.BreakDurationSeconds),
                _dateTimeProvider,
                _logger));
    }
}

public sealed class ProductLookupCircuitBreaker : IProductLookupCircuitBreaker
{
    private readonly string _providerName;
    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Microsoft.Extensions.Logging.ILogger<ProductLookupCircuitBreaker> _logger;
    private readonly object _lock = new();

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _lastStateChangeUtc;
    private bool _halfOpenProbeInProgress;

    public ProductLookupCircuitBreaker(
        string providerName,
        int failureThreshold,
        TimeSpan breakDuration,
        IDateTimeProvider dateTimeProvider,
        Microsoft.Extensions.Logging.ILogger<ProductLookupCircuitBreaker> logger)
    {
        _providerName = providerName;
        _failureThreshold = failureThreshold > 0 ? failureThreshold : 3;
        _breakDuration = breakDuration > TimeSpan.Zero ? breakDuration : TimeSpan.FromSeconds(30);
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _lastStateChangeUtc = dateTimeProvider.UtcNow;
    }

    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                CheckStateTransition_NoLock();
                return _state;
            }
        }
    }

    public bool TryExecute(out string? rejectionReason)
    {
        lock (_lock)
        {
            CheckStateTransition_NoLock();

            if (_state == CircuitState.Open)
            {
                rejectionReason = "provider_unavailable";
                return false;
            }

            if (_state == CircuitState.HalfOpen)
            {
                if (_halfOpenProbeInProgress)
                {
                    rejectionReason = "provider_unavailable";
                    return false;
                }

                _halfOpenProbeInProgress = true;
                rejectionReason = null;
                return true;
            }

            rejectionReason = null;
            return true;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
                _consecutiveFailures = 0;
                _halfOpenProbeInProgress = false;
                _lastStateChangeUtc = _dateTimeProvider.UtcNow;

                _logger.LogInformation(
                    "Circuit breaker for provider {Provider} transitioned from HALF-OPEN to CLOSED after successful probe.",
                    _providerName);
            }
            else if (_state == CircuitState.Closed)
            {
                _consecutiveFailures = 0;
            }
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            var now = _dateTimeProvider.UtcNow;

            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _halfOpenProbeInProgress = false;
                _lastStateChangeUtc = now;

                _logger.LogWarning(
                    "Circuit breaker for provider {Provider} probe failed. Transitioned from HALF-OPEN to OPEN for {DurationSeconds}s.",
                    _providerName,
                    _breakDuration.TotalSeconds);
            }
            else if (_state == CircuitState.Closed)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _lastStateChangeUtc = now;

                    _logger.LogWarning(
                        "Circuit breaker for provider {Provider} reached failure threshold ({Failures}). Transitioned to OPEN for {DurationSeconds}s.",
                        _providerName,
                        _consecutiveFailures,
                        _breakDuration.TotalSeconds);
                }
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _consecutiveFailures = 0;
            _halfOpenProbeInProgress = false;
            _lastStateChangeUtc = _dateTimeProvider.UtcNow;
        }
    }

    private void CheckStateTransition_NoLock()
    {
        if (_state == CircuitState.Open)
        {
            var elapsed = _dateTimeProvider.UtcNow - _lastStateChangeUtc;
            if (elapsed >= _breakDuration)
            {
                _state = CircuitState.HalfOpen;
                _halfOpenProbeInProgress = false;
                _lastStateChangeUtc = _dateTimeProvider.UtcNow;

                _logger.LogInformation(
                    "Circuit breaker for provider {Provider} transitioned from OPEN to HALF-OPEN after break duration ({DurationSeconds}s).",
                    _providerName,
                    _breakDuration.TotalSeconds);
            }
        }
    }
}
