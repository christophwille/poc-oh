using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;

namespace DacPacDeployer
{
    public class Deployer
    {
        private readonly ILogger _logger;

        public Deployer(ILogger logger)
        {
            _logger = logger;
        }

        // https://github.com/microsoft/DacFx
        // https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/concepts/data-tier-applications/deploy-data-tier-application
        public DeploymentResult DeployDacPac(string serverConnString,
            string database,
            Stream dacpacStream,
            CancellationToken ct = default(CancellationToken))
        {
            var dacOptions = new DacDeployOptions()
            {
                BlockOnPossibleDataLoss = true, // default: true
                IncludeTransactionalScripts = true,  // default: false - "Rollback on failure"
                                                     // BackupDatabaseBeforeChanges = true,
                                                     // VerifyDeployment = true // default: true
            };

            var messageList = new List<string>();

            try
            {
                var dacServiceInstance = new DacServices(serverConnString);

                // dacServiceInstance.ProgressChanged += (s, e) => messageList.Add(e.Message);
                dacServiceInstance.Message += (s, e) => messageList.Add(e.Message.Message);

                using (DacPackage dacpac = DacPackage.Load(dacpacStream))
                {
                    dacServiceInstance.Deploy(dacpac, database,
                        upgradeExisting: true,
                        options: dacOptions,
                        cancellationToken: ct);
                }

                return new DeploymentResult { Succeeded = true, Messages = messageList };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
                return new DeploymentResult { Succeeded = false, Exception = $"Message: {ex.Message} Inner Exception: {ex.InnerException?.Message} StackTrace: {ex.StackTrace?.ToString()}", Messages = messageList };
            }
        }

        public DeploymentReportResult GenerateDeployReport(string serverConnString,
            string database,
            Stream dacpacStream,
            CancellationToken ct = default(CancellationToken))
        {
            var dacOptions = new DacDeployOptions()
            {
                BlockOnPossibleDataLoss = true,
            };

            try
            {
                var dacServiceInstance = new DacServices(serverConnString);

                using (DacPackage dacpac = DacPackage.Load(dacpacStream))
                {
                    string xmlReport = dacServiceInstance.GenerateDeployReport(dacpac, database, dacOptions, ct);
                    return new DeploymentReportResult { Succeeded = true, XmlResult = xmlReport };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
                return new DeploymentReportResult { Succeeded = false, Exception = ex.Message };
            }
        }
    }
}
