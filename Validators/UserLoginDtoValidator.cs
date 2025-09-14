using FluentValidation;
using UserAuthApi.Models;

namespace UserAuthApi.Validators
{
    public class UserLoginDtoValidator : AbstractValidator<UserLoginDto>
    {
        public UserLoginDtoValidator() 
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}
