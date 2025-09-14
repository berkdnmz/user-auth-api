using FluentValidation.Results;

namespace UserAuthApi.Extensions
{
    public static class ValidationExtensions
    {
        public static Dictionary<string, string[]> ToDictionary(this ValidationResult validationResult)
        {
            return validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }
    }
}