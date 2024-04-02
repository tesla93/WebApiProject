using DataProcessing.Classes;
using System;
using System.Globalization;
using System.Linq;

namespace DataProcessing.Validation
{
    public class DateTimeOffsetValidator : EntryCellValidator
    {
        private readonly string _dateFormat = CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern;

        /// <summary>
        /// Custom constructor
        /// </summary>
        /// <param name="typeInfo">type info</param>
        public DateTimeOffsetValidator(DateTimeCellDataTypeInfo typeInfo = null)
        {
            if (typeInfo == null) return;
            if (!string.IsNullOrWhiteSpace(typeInfo.DateFormats))
            {
                _dateFormat = $"{_dateFormat}, {typeInfo.DateFormats}";
            }
        }

        /// <summary>
        /// Custom constructor
        /// </summary>
        /// <param name="typeInfo">type info</param>
        public override void PerformValidation(ImportEntryCell cell)
        {
            DateTimeOffset dateTimeOffset;
            bool isValid;

            var formats = _dateFormat.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).Distinct().ToArray();
            isValid = DateTimeOffset.TryParseExact(Convert.ToString(cell.Value), formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dateTimeOffset);
            // isValid = DateTime.TryParse(Convert.ToString(cell.Value),out dateTime);

            if (isValid)
            {
                cell.Value = dateTimeOffset;
            }
            else
            {

                cell.ErrorMessage = $"The value \"{cell.Value.ToString().Trim()}\" does not match the specified date formats: {string.Join(", ", formats.Select(f => $"'{f}'"))}";
            }
        }
    }
}
