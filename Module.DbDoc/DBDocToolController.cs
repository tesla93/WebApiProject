using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Module.Core.Filters;
using Module.Core.Membership;
using Module.Core.Membership.Services;
using Module.DbDoc.Model;
using Module.DbDoc.Services;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Module.DbDoc
{
    [Route("api/db-doc")]
    [Authorize(Roles = Roles.SuperAdminRole)]
    public class DBDocToolController : Module.Core.Web.ControllerBase
    {
        private readonly IDbDocToolService _dbDocToolService;
        private readonly IDbMetadataService _dbMetadataService;
        private readonly IUserService _userService;

        public DBDocToolController(
            IDbDocToolService dbDocToolService,
            IDbMetadataService dbMetadataService,
            IUserService userService,
            ILogger<DBDocToolController> logger) : base(logger)
        {
            _userService = userService;
            _dbDocToolService = dbDocToolService;
            _dbMetadataService = dbMetadataService;
        }

        [HttpGet, Route(""), ResponseCache(NoStore = true)]
        public IActionResult Get() =>
            Ok(_dbDocToolService.GetSyncedStructure());

        [HttpGet, Route("types"), ResponseCache(NoStore = true)]
        public List<DbColumnType> GetTypes()
        {
            return _dbDocToolService.GetColumnTypes() ?? new List<DbColumnType>();
        }

        [HttpGet, Route("table-dump"), ResponseCache(NoStore = true)]
        public JsonResult GetDump(string tablename, FilterCommand filter)
        {
            var provider = _dbDocToolService.GetDbTableDumpProvider(tablename);
            return Json(new
            {
                Cols = provider.GetTableColumnsList(tablename),
                Data = provider.GetTableDump(tablename, filter)
            });
        }

        [HttpPost, Route(""), ResponseCache(NoStore = true)]
        public async Task Save([FromBody]DbStructure db)
        {
            var user = await _userService.Get(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _dbDocToolService.SyncAndSaveStructure(db);
            await _dbDocToolService.SendToGit(user.Email, new CancellationToken());
        }

        [HttpPost, Route("types"), ResponseCache(NoStore = true)]
        public async Task Save([FromBody]List<DbColumnType> items)
        {
            var user = await _userService.Get(User.FindFirstValue(ClaimTypes.NameIdentifier));
            items.Where(i => i.Id == null).ToList().ForEach(i => i.Id = Guid.NewGuid());
            _dbDocToolService.SaveItemTypes(items);
            await _dbDocToolService.SendToGit(user.Email, new CancellationToken());
        }

        [HttpGet, Route("xml"), ResponseCache(NoStore = true)]
        public FileContentResult GetFile()
        {
            var db = _dbDocToolService.GetSyncedStructure();
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Auto }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("configuration");
                    writer.WriteAttributeString("jdbcurl", string.Empty);

                    db.Tables.ForEach(tbl =>
                    {
                        writer.WriteStartElement("table");
                        writer.WriteAttributeString("name", tbl.Name);
                        tbl.Columns.ForEach(col =>
                        {
                            if (col.AnonRule == null) return;

                            writer.WriteStartElement("column");
                            writer.WriteAttributeString("name", col.Name);
                            writer.WriteAttributeString("type", col.AnonRule.ToString());
                            writer.WriteEndElement();
                        });
                        writer.WriteEndElement();
                    });

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                }

                return File(memoryStream.ToArray(), "text/xml", "anonrule.xml");
            }
        }

        [HttpGet, Route("metadata")]
        [AllowAnonymous]
        public IActionResult GetAllMetadata() =>
            Ok(_dbMetadataService.GetAllMetadata());
    }
}