namespace App.Web.Features.Upload.ControlPlane;

public static class UploadControlPlaneSessionFactory
{
    public static UploadControlPlaneSignInRequest CreateSignInRequest(
        UploadControlPlaneSignInFormModel form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new UploadControlPlaneSignInRequest
        {
            Login = Required(form.Login, nameof(form.Login)),
            Password = Required(form.Password, nameof(form.Password))
        };
    }

    public static UploadControlPlaneSession CreateSession(
        UploadControlPlaneSignInResponse response,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new UploadControlPlaneSession(
            response.EffectiveUserId,
            response.EffectiveDisplayName,
            Required(response.AccessToken, nameof(response.AccessToken)),
            response.RefreshToken,
            createdAtUtc);
    }

    private static string Required(string? value, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"{fieldName} is required.");
    }
}