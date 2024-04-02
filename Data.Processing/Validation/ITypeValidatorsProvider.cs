using AutofacExtensions;
using DataProcessing.Classes;

namespace DataProcessing.Validation
{
    /// <summary>
    /// Provides validator for the column definition's type 
    /// </summary>
    public interface ITypeValidatorsProvider
    {
        /// <summary>
        /// Returns validator for the column definition's type
        /// </summary>
        /// <param name="columnDefinition">Column definition</param>
        /// <returns>Validator instance</returns>
        [IgnoreLogging]
        EntryCellValidator GetValidator(ColumnDefinition columnDefinition);
    }
}