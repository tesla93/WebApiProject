using Core.Exceptions;
using Microsoft.EntityFrameworkCore.Internal;

namespace FileStorage
{
    public sealed class FilesUploadingException : DataException
    {
        public FilesUploadingException(FilesUploadingResult uploadingResult)
            : base($"Files uploading error occurred with next files: {uploadingResult.FailedUploadedFileNames}") =>
            UploadingResult = uploadingResult;


        public FilesUploadingResult UploadingResult { get; }
    }
}
