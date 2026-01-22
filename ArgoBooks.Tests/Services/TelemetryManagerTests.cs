using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the TelemetryManager and related services.
/// </summary>
public class TelemetryManagerTests
{
    #region TelemetryEvent Tests

    [Fact]
    public void TelemetryEvent_GeneratesUniqueDataId()
    {
        var event1 = new SessionEvent();
        var event2 = new SessionEvent();

        Assert.NotEqual(event1.DataId, event2.DataId);
        Assert.Equal(16, event1.DataId.Length);
    }

    [Fact]
    public void TelemetryEvent_SetsTimestampToUtcNow()
    {
        var before = DateTime.UtcNow;
        var telemetryEvent = new SessionEvent();
        var after = DateTime.UtcNow;

        Assert.InRange(telemetryEvent.Timestamp, before, after);
    }

    [Fact]
    public void SessionEvent_HasCorrectDataType()
    {
        var sessionEvent = new SessionEvent();
        Assert.Equal(TelemetryDataType.Session, sessionEvent.DataType);
    }

    [Fact]
    public void ExportEvent_HasCorrectDataType()
    {
        var exportEvent = new ExportEvent();
        Assert.Equal(TelemetryDataType.Export, exportEvent.DataType);
    }

    [Fact]
    public void ApiUsageEvent_HasCorrectDataType()
    {
        var apiEvent = new ApiUsageEvent();
        Assert.Equal(TelemetryDataType.ApiUsage, apiEvent.DataType);
    }

    [Fact]
    public void ErrorEvent_HasCorrectDataType()
    {
        var errorEvent = new ErrorEvent();
        Assert.Equal(TelemetryDataType.Error, errorEvent.DataType);
    }

    [Fact]
    public void FeatureUsageEvent_HasCorrectDataType()
    {
        var featureEvent = new FeatureUsageEvent();
        Assert.Equal(TelemetryDataType.FeatureUsage, featureEvent.DataType);
    }

    #endregion

    #region GeoLocationData Tests

    [Fact]
    public void GeoLocationData_HasDefaultValues()
    {
        var geoData = new GeoLocationData();

        Assert.Equal("Unknown", geoData.Country);
        Assert.Equal("Unknown", geoData.CountryCode);
        Assert.Equal("Unknown", geoData.Region);
        Assert.Equal("Unknown", geoData.City);
        Assert.Equal("Unknown", geoData.Timezone);
        Assert.Null(geoData.HashedIp);
    }

    #endregion

    #region Event Property Tests

    [Fact]
    public void SessionEvent_CanSetAllProperties()
    {
        var sessionEvent = new SessionEvent
        {
            Action = SessionAction.SessionStart,
            DurationSeconds = 3600,
            AppVersion = "1.0.0",
            Platform = "Windows",
            UserAgent = "Windows 10 (x64)"
        };

        Assert.Equal(SessionAction.SessionStart, sessionEvent.Action);
        Assert.Equal(3600, sessionEvent.DurationSeconds);
        Assert.Equal("1.0.0", sessionEvent.AppVersion);
        Assert.Equal("Windows", sessionEvent.Platform);
    }

    [Fact]
    public void ExportEvent_CanSetAllProperties()
    {
        var exportEvent = new ExportEvent
        {
            ExportType = ExportType.Excel,
            DurationMs = 1500,
            FileSize = 102400
        };

        Assert.Equal(ExportType.Excel, exportEvent.ExportType);
        Assert.Equal(1500, exportEvent.DurationMs);
        Assert.Equal(102400, exportEvent.FileSize);
    }

    [Fact]
    public void ApiUsageEvent_CanSetAllProperties()
    {
        var apiEvent = new ApiUsageEvent
        {
            ApiName = ApiName.OpenAI,
            Model = "gpt-4o-mini",
            DurationMs = 2500,
            TokensUsed = 150,
            Success = true
        };

        Assert.Equal(ApiName.OpenAI, apiEvent.ApiName);
        Assert.Equal("gpt-4o-mini", apiEvent.Model);
        Assert.Equal(2500, apiEvent.DurationMs);
        Assert.Equal(150, apiEvent.TokensUsed);
        Assert.True(apiEvent.Success);
    }

    [Fact]
    public void ErrorEvent_CanSetAllProperties()
    {
        var errorEvent = new ErrorEvent
        {
            ErrorCode = "InvalidOperationException",
            ErrorCategory = ErrorCategory.Validation,
            Message = "Test error message",
            SourceFile = "TestFile.cs",
            LineNumber = 42,
            MethodName = "TestMethod"
        };

        Assert.Equal("InvalidOperationException", errorEvent.ErrorCode);
        Assert.Equal(ErrorCategory.Validation, errorEvent.ErrorCategory);
        Assert.Equal("Test error message", errorEvent.Message);
        Assert.Equal("TestFile.cs", errorEvent.SourceFile);
        Assert.Equal(42, errorEvent.LineNumber);
        Assert.Equal("TestMethod", errorEvent.MethodName);
    }

    [Fact]
    public void FeatureUsageEvent_CanSetAllProperties()
    {
        var featureEvent = new FeatureUsageEvent
        {
            FeatureName = FeatureName.ChartViewed,
            Context = "Revenue by Category"
        };

        Assert.Equal(FeatureName.ChartViewed, featureEvent.FeatureName);
        Assert.Equal("Revenue by Category", featureEvent.Context);
    }

    #endregion

    #region Enum Coverage Tests

    [Theory]
    [InlineData(ExportType.Excel)]
    [InlineData(ExportType.GoogleSheets)]
    [InlineData(ExportType.Pdf)]
    [InlineData(ExportType.Csv)]
    [InlineData(ExportType.Backup)]
    [InlineData(ExportType.Receipts)]
    [InlineData(ExportType.ChartImage)]
    public void ExportType_AllValuesAreDefined(ExportType exportType)
    {
        var exportEvent = new ExportEvent { ExportType = exportType };
        Assert.Equal(exportType, exportEvent.ExportType);
    }

    [Theory]
    [InlineData(ApiName.OpenAI)]
    [InlineData(ApiName.OpenExchangeRates)]
    [InlineData(ApiName.GoogleSheets)]
    [InlineData(ApiName.AzureDocumentIntelligence)]
    [InlineData(ApiName.MicrosoftTranslator)]
    public void ApiName_AllValuesAreDefined(ApiName apiName)
    {
        var apiEvent = new ApiUsageEvent { ApiName = apiName };
        Assert.Equal(apiName, apiEvent.ApiName);
    }

    [Theory]
    [InlineData(ErrorCategory.Unknown)]
    [InlineData(ErrorCategory.Network)]
    [InlineData(ErrorCategory.FileSystem)]
    [InlineData(ErrorCategory.Parsing)]
    [InlineData(ErrorCategory.Validation)]
    [InlineData(ErrorCategory.UI)]
    [InlineData(ErrorCategory.Api)]
    [InlineData(ErrorCategory.Export)]
    [InlineData(ErrorCategory.Import)]
    [InlineData(ErrorCategory.License)]
    [InlineData(ErrorCategory.Authentication)]
    [InlineData(ErrorCategory.Encryption)]
    public void ErrorCategory_AllValuesAreDefined(ErrorCategory category)
    {
        var errorEvent = new ErrorEvent { ErrorCategory = category };
        Assert.Equal(category, errorEvent.ErrorCategory);
    }

    #endregion

    #region TelemetryStatistics Tests

    [Fact]
    public void TelemetryStatistics_HasDefaultValues()
    {
        var stats = new TelemetryStatistics();

        Assert.Equal(0, stats.TotalEvents);
        Assert.Equal(0, stats.PendingEvents);
        Assert.Equal(0, stats.UploadedEvents);
        Assert.NotNull(stats.EventsByType);
        Assert.Empty(stats.EventsByType);
        Assert.Null(stats.OldestEventTime);
        Assert.Null(stats.NewestEventTime);
        Assert.Null(stats.LastUploadTime);
        Assert.Equal(0, stats.TotalEventsEverUploaded);
    }

    #endregion

    #region TelemetryUploadResult Tests

    [Fact]
    public void TelemetryUploadResult_HasDefaultValues()
    {
        var result = new TelemetryUploadResult();

        Assert.False(result.Success);
        Assert.Equal(0, result.EventsUploaded);
        Assert.Equal(0, result.TotalPending);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TelemetryUploadResult_CanSetSuccessState()
    {
        var result = new TelemetryUploadResult
        {
            Success = true,
            EventsUploaded = 50,
            TotalPending = 50
        };

        Assert.True(result.Success);
        Assert.Equal(50, result.EventsUploaded);
    }

    [Fact]
    public void TelemetryUploadResult_CanSetErrorState()
    {
        var result = new TelemetryUploadResult
        {
            Success = false,
            ErrorMessage = "Network error",
            EventsUploaded = 10,
            TotalPending = 100
        };

        Assert.False(result.Success);
        Assert.Equal("Network error", result.ErrorMessage);
        Assert.Equal(10, result.EventsUploaded);
        Assert.Equal(100, result.TotalPending);
    }

    #endregion
}
