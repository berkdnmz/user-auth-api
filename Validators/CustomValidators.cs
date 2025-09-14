using FluentValidation;
using UserAuthApi.Data;
using Microsoft.EntityFrameworkCore;

namespace UserAuthApi.Validators
{
    public static class CustomValidators
    {
        public static IRuleBuilderOptions<T, string> MustBeUniqueUsername<T>(
            this IRuleBuilder<T, string> ruleBuilder, AppDbContext context)
        {
            return ruleBuilder
                .MustAsync(async (username, cancellationToken) =>
                {
                    return !await context.Users.AnyAsync(u => u.Username == username);
                })
                .WithMessage("Username already exists");
        }

        public static IRuleBuilderOptions<T, string> MustBeUniqueEmail<T>(
            this IRuleBuilder<T, string> ruleBuilder, AppDbContext context)
        {
            return ruleBuilder
                .MustAsync(async (email, cancellationToken) =>
                {
                    return !await context.Users.AnyAsync(u => u.Email == email);
                })
                .WithMessage("Email already exists");
        }
    }
}