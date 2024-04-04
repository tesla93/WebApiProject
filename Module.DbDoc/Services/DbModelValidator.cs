using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Module.DbDoc.Enums;
using Module.DbDoc.Model.ValidationMetadata;
using PhoneNumbers;

namespace Module.DbDoc.Services
{
    public class DbModelValidator : IDbModelValidator
    {
        public bool Validate(ValidationRule rule, object value)
        {
            return rule.AcceptValidator(this, value);
        }

        public bool Validate(RequiredValidationRule rule, object value)
        {
            return value != null;
        }

        public bool Validate(NumberRangeValidationRule rule, double value)
        {
            return ValidateRangeRule(rule, value);
        }

        public bool Validate(DateRangeValidationRule rule, DateTimeOffset value)
        {
            return ValidateRangeRule(rule, value);
        }

        private static bool ValidateRangeRule<T>(RangeValidationRule<T> rule, T value) where T : struct
        {
            return (!rule.Min.HasValue || Comparer<T>.Default.Compare(rule.Min.Value, value) <= 0) &&
                   (!rule.Max.HasValue || Comparer<T>.Default.Compare(rule.Max.Value, value) >= 0);
        }

        public bool Validate(InputFormatValidationRule rule, string value)
        {
            switch (rule.Type)
            {
                case InputFormatType.Phone:
                    return ValidatePhone();
                case InputFormatType.Email:
                    return ValidateEmail();
                case InputFormatType.Url:
                    return ValidateUrl();
                case InputFormatType.Regex:
                    return ValidateByRegex(rule.Format);
            }

            return false;

            bool ValidatePhone()
            {
                var phoneNumber = Convert.ToString(value);
                var util = PhoneNumberUtil.GetInstance();

                try
                {
                    var phone = util.Parse(phoneNumber, "GB");
                    return util.IsValidNumber(phone);
                }
                catch
                {
                    return false;
                }
            }

            bool ValidateEmail()
            {
                return new EmailAddressAttribute().IsValid(value);
            }

            bool ValidateUrl()
            {
                return Uri.TryCreate(Convert.ToString(value), UriKind.Absolute, out _);
            }

            bool ValidateByRegex(string pattern)
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                return regex.IsMatch(value);
            }
        }

        public bool Validate(MaxLengthValidationRule rule, object value)
        {
            return new MaxLengthAttribute(rule.MaxLength).IsValid(value);
        }
    }
}