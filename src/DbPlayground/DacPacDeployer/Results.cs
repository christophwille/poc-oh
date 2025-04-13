using System.Xml.Serialization;
using DacPacDeployer.Schema;

namespace DacPacDeployer
{
    public class ResultBase
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }

    public class DeploymentResult : ResultBase
    {
        public List<string> Messages { get; set; }
    }

    public class DeploymentReportResult : ResultBase
    {
        public string XmlResult { get; set; }

        public DeploymentReport DeserializeReport()
        {
            if (String.IsNullOrWhiteSpace(this.XmlResult)) return null;

            XmlSerializer serializer = new XmlSerializer(typeof(DeploymentReport));

            using (TextReader reader = new StringReader(this.XmlResult))
            {
                var result = (DeploymentReport)serializer.Deserialize(reader);
                return result;
            }
        }
    }
}
