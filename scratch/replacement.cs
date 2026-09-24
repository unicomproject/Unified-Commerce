    public async Task<ApplicationResult<ReportExportDto>> CreateExportAsync(
        TenantRequestContext context,
        ReportExportRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return ApplicationResult<ReportExportDto>.Failure(error);
        if (!await ReportFeaturePolicy.IsExportEnabledAsync(_entitlements, context.TenantId, _clock.UtcNow, cancellationToken) ||
            !HasAnyPermission(context, TenantAdminReportPermissions.Export))
        {
            return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
        }
        
        if (request.Format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) || request.Format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ReportExportDto>.Failure(new ApplicationError("reports.format_not_supported", "Format is deferred/unsupported in Release 1."));
        }
        if (!request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ReportExportDto>.Failure(new ApplicationError("reports.format_invalid", "Invalid format."));
        }

        if (!await CanViewExportTargetAsync(context, request.ReportType, request.Section, cancellationToken))
        {
            return ApplicationResult<ReportExportDto>.Failure(PermissionDenied);
        }

        var filters = request.Filters with { Page = 1, PageSize = int.MaxValue };
        ReportResultDto? reportResult = request.ReportType.ToLowerInvariant() switch
        {
            "sales" => await _repository.GetSalesAsync(context, filters, cancellationToken),
            "stock" => await _repository.GetStockAsync(context, filters, cancellationToken),
            "outlets" => await _repository.GetOutletsAsync(context, filters, cancellationToken),
            _ => null
        };

        if (reportResult == null)
        {
            return ApplicationResult<ReportExportDto>.Failure(NotFound);
        }

        var csvBytes = CsvGenerator.Generate(reportResult.Records);

        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var job = new ReportExportDto(
            jobId,
            request.ReportType.Trim().ToLowerInvariant(),
            request.Section.Trim(),
            request.Format.Trim().ToUpperInvariant(),
            "COMPLETED",
            now,
            now,
            BuildSafeFileName(request.ReportType, request.Section, request.Format),
            $"/api/v1/tenant/reports/exports/{jobId}/download",
            now.AddMinutes(15),
            null);
            
        var entry = new ExportJobEntry(job, context.TenantId, context.UserId, csvBytes);
        ExportJobs[jobId] = entry;

        return ApplicationResult<ReportExportDto>.Success(job);
    }

    public Task<ApplicationResult<ReportExportDto>> GetExportAsync(
        TenantRequestContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return Task.FromResult(ApplicationResult<ReportExportDto>.Failure(error));
        
        if (ExportJobs.TryGetValue(jobId, out var entry) && entry.TenantId == context.TenantId && entry.UserId == context.UserId)
        {
            return Task.FromResult(ApplicationResult<ReportExportDto>.Success(entry.Dto));
        }
        
        return Task.FromResult(ApplicationResult<ReportExportDto>.Failure(NotFound));
    }

    public Task<ApplicationResult<byte[]>> DownloadExportAsync(
        TenantRequestContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var error = ValidateCommonAccess(context);
        if (error is not null) return Task.FromResult(ApplicationResult<byte[]>.Failure(error));
        
        if (ExportJobs.TryGetValue(jobId, out var entry) && entry.TenantId == context.TenantId && entry.UserId == context.UserId && entry.Data != null)
        {
            return Task.FromResult(ApplicationResult<byte[]>.Success(entry.Data));
        }
        
        return Task.FromResult(ApplicationResult<byte[]>.Failure(NotFound));
    }
