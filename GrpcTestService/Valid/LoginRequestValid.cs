using FluentValidation;
using My.GRPC.Demo;

namespace GrpcTestService.Valid;

public class LoginRequestValid: AbstractValidator<LoginRequest>
{
    public LoginRequestValid()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("账号不能为空");
        RuleFor(x => x.Password).NotEmpty().WithMessage("密码不能为空");
    }
}