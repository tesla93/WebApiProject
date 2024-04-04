using System;
using System.Collections.Generic;
using Module.DbDoc.Model;

namespace Module.DbDoc.Services
{
    public interface IDbMetadataService
    {
        List<DbTableMetadata> GetAllMetadata();
        List<DbMetadataFieldResult> GetMetadata<DTOType>(string url = null);
        List<DbMetadataFieldResult> GetMetadata(Type dtoType, string url = null);
    }
}