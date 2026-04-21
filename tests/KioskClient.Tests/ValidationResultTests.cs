using KioskClient.Core.Models;
using Xunit;

namespace KioskClient.Tests;

public class ValidationResultTests
{
    [Fact]
    public void IsFullyValid_AllValid_ReturnsTrue()
    {
        var result = new ValidationResult
        {
            Identifier = "Root",
            IsValid = true,
            Children =
            [
                new() { Identifier = "Child1", IsValid = true },
                new() { Identifier = "Child2", IsValid = true }
            ]
        };

        Assert.True(result.IsFullyValid);
    }

    [Fact]
    public void IsFullyValid_ChildInvalid_ReturnsFalse()
    {
        var result = new ValidationResult
        {
            Identifier = "Root",
            IsValid = true,
            Children =
            [
                new() { Identifier = "Child1", IsValid = true },
                new() { Identifier = "Child2", IsValid = false }
            ]
        };

        Assert.False(result.IsFullyValid);
    }

    [Fact]
    public void IsFullyValid_RootInvalid_ReturnsFalse()
    {
        var result = new ValidationResult
        {
            Identifier = "Root",
            IsValid = false,
            Children =
            [
                new() { Identifier = "Child1", IsValid = true }
            ]
        };

        Assert.False(result.IsFullyValid);
    }

    [Fact]
    public void PassedCount_CountsAllValidRecursively()
    {
        var result = new ValidationResult
        {
            Identifier = "Root",
            IsValid = true,
            Children =
            [
                new()
                {
                    Identifier = "Child1",
                    IsValid = true,
                    Children =
                    [
                        new() { Identifier = "Grandchild", IsValid = true }
                    ]
                },
                new() { Identifier = "Child2", IsValid = false }
            ]
        };

        Assert.Equal(3, result.PassedCount); // Root + Child1 + Grandchild
    }

    [Fact]
    public void FailedCount_CountsAllInvalidRecursively()
    {
        var result = new ValidationResult
        {
            Identifier = "Root",
            IsValid = false,
            Children =
            [
                new() { Identifier = "Child1", IsValid = true },
                new()
                {
                    Identifier = "Child2",
                    IsValid = false,
                    Children =
                    [
                        new() { Identifier = "Grandchild", IsValid = false }
                    ]
                }
            ]
        };

        Assert.Equal(3, result.FailedCount); // Root + Child2 + Grandchild
    }

    [Fact]
    public void IsFullyValid_EmptyChildren_UsesOwnState()
    {
        var valid = new ValidationResult { IsValid = true };
        var invalid = new ValidationResult { IsValid = false };

        Assert.True(valid.IsFullyValid);
        Assert.False(invalid.IsFullyValid);
    }

    [Fact]
    public void NullIsValid_TreatedAsInvalid()
    {
        var result = new ValidationResult { IsValid = null };

        Assert.False(result.IsFullyValid);
        Assert.Equal(0, result.PassedCount);
        Assert.Equal(0, result.FailedCount);
    }
}
