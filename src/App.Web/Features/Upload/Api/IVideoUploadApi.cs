using BuildingBlocks.Contracts.VideoUpload;

namespace App.Web.Features.Upload.Api;

public interface IVideoUploadApi
{
    Task<VideoPreUploadCheckResponseDto> CheckPreUploadAsync(
        VideoPreUploadCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<VideoUploadReceiptResponseDto> SubmitUploadReceiptAsync(
        VideoUploadReceiptRequestDto request,
        CancellationToken cancellationToken = default);
}