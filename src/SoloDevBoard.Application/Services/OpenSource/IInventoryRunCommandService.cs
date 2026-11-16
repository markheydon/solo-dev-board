namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Manual inventory scan requests from the web app.</summary>
public interface IInventoryRunCommandService
{
    Task<RequestInventoryRunResultDto> RequestScanNowAsync(CancellationToken cancellationToken);
}