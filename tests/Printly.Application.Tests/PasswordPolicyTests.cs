using Printly.Application.Services;
using Printly.Domain.Exceptions;

namespace Printly.Application.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Abcdef1!")]
    [InlineData("Ação1234!")]
    [InlineData("Abc def1")]
    public void Validate_AcceptsStrongPasswords(string password)
    {
        PasswordPolicy.Validate(password);
    }

    [Fact]
    public void Validate_Accepts72CharAsciiPassword()
    {
        PasswordPolicy.Validate("Aa1!" + new string('x', 68));
    }

    [Fact]
    public void Validate_Rejects73CharAsciiPassword()
    {
        var ex = Assert.Throws<DomainException>(() => PasswordPolicy.Validate("Aa1!" + new string('x', 69)));
        Assert.Equal("A senha deve ter no maximo 72 bytes.", ex.Message);
    }

    [Fact]
    public void Validate_Rejects_AccentedPasswordOver72BytesButUnder72Chars()
    {
        var password = "Aa1!" + new string('ã', 40);
        Assert.True(password.Length < 72);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(password) > 72);

        var ex = Assert.Throws<DomainException>(() => PasswordPolicy.Validate(password));
        Assert.Equal("A senha deve ter no maximo 72 bytes.", ex.Message);
    }

    [Theory]
    [InlineData("abcdef1!", "A senha deve ter uma letra maiuscula.")]
    [InlineData("ABCDEF1!", "A senha deve ter uma letra minuscula.")]
    [InlineData("Abcdefg!", "A senha deve ter um numero.")]
    [InlineData("Abcdefg1", "A senha deve ter um caractere especial.")]
    [InlineData("Abc1!", "A senha deve ter ao menos 8 caracteres.")]
    [InlineData("abcdefgh", "A senha deve ter uma letra maiuscula, um numero e um caractere especial.")]
    [InlineData("", "A senha deve ter ao menos 8 caracteres, uma letra maiuscula, uma letra minuscula, um numero e um caractere especial.")]
    public void Validate_ListsEveryMissingItem(string password, string expected)
    {
        var ex = Assert.Throws<DomainException>(() => PasswordPolicy.Validate(password));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void Validate_NullCountsAsEmpty()
    {
        var ex = Assert.Throws<DomainException>(() => PasswordPolicy.Validate(null!));
        Assert.Equal("A senha deve ter ao menos 8 caracteres, uma letra maiuscula, uma letra minuscula, um numero e um caractere especial.", ex.Message);
    }
}
