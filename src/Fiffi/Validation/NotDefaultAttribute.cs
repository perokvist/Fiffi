using System;
using System.ComponentModel.DataAnnotations;

namespace Fiffi.Validation;

[AttributeUsage(
   AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
   AllowMultiple = false)]
public class NotDefaultAttribute : ValidationAttribute
{
    public const string DefaultErrorMessage = "The {0} field must not be default";
    public NotDefaultAttribute() : base(DefaultErrorMessage) { }

    public override bool IsValid(object value)
        => !value.GetType().GetDefault().Equals(value);

}
