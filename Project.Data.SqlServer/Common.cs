using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;

namespace Data.SqlServer
{
    public class Common
    {
        //runs sql file. Make the file embedded resource before - see its properties-> build action.
        public static void RunSqlFile(string fileName, MigrationBuilder migrationBuilder) {
            var assembly = Assembly.GetExecutingAssembly();

            var resName = assembly.GetManifestResourceNames().Single(i => i.EndsWith(fileName));
            using (Stream stream = assembly.GetManifestResourceStream(resName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                migrationBuilder.Sql(result);
                //var s1 = result.Substring(0, 10);
            }
        }


        // Copy Embedded resource into temp file and return its name
        public static string CopyResourceToTempFilename(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resName = assembly.GetManifestResourceNames().Single(i => i.EndsWith(fileName));
            using (Stream stream = assembly.GetManifestResourceStream(resName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                string strTempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");

                File.WriteAllText(strTempFile, result);
                return strTempFile;
            }
        }

        // Copy Embedded resource into temp file and return its name
        public static string ReturnFileContent(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resName = assembly.GetManifestResourceNames().Single(i => i.EndsWith(fileName));
            using (Stream stream = assembly.GetManifestResourceStream(resName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
