namespace FixMyTownApi.Models.Dtos.Common
{
    /// <summary>
    /// A tiny, reusable shape for plain confirmation/error messages -
    /// e.g. { "message": "Report submitted successfully." }
    ///
    /// NOTE ON CASING: "message" is lowercase-first on purpose - the
    /// frontend reads err.response?.data?.message everywhere it
    /// handles an API error, so the JSON key must match exactly.
    /// </summary>
    public class ApiMessageDto
    {
        public string message { get; set; } = string.Empty;

        public ApiMessageDto() { }
        public ApiMessageDto(string text) => message = text;
    }
}
