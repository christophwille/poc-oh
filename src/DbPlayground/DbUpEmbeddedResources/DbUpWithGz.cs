using System.Reflection;
using System.Text;
using DbUp.Builder;
using DbUp.Engine.Transactions;
using DbUp.Engine;
using DbUp;
using System.IO.Compression;

namespace DbUpEmbeddedResources
{
    public static class GzProviderExtensions
    {
        public static UpgradeEngineBuilder WithGzippedScriptsEmbeddedInAssembly(this UpgradeEngineBuilder builder, Assembly assembly)
        {
            return WithScriptsLocal(builder, new GzEmbeddedScriptProvider(assembly, s => s.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase)));
        }

        public static UpgradeEngineBuilder WithScriptsLocal(this UpgradeEngineBuilder builder, IScriptProvider scriptProvider)
        {
            builder.Configure(c => c.ScriptProviders.Add(scriptProvider));
            return builder;
        }
    }

    /// <summary>
    /// The default <see cref="IScriptProvider"/> implementation which retrieves upgrade scripts embedded in an assembly.
    /// </summary>
    public class GzEmbeddedScriptProvider : GzEmbeddedScriptsProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GzEmbeddedScriptProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="filter">The filter.</param>
        public GzEmbeddedScriptProvider(Assembly assembly, Func<string, bool> filter) : this(assembly, filter, DbUpDefaults.DefaultEncoding, new SqlScriptOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GzEmbeddedScriptProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="encoding">The encoding.</param>
        public GzEmbeddedScriptProvider(Assembly assembly, Func<string, bool> filter, Encoding encoding) : this(assembly, filter, encoding, new SqlScriptOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GzEmbeddedScriptProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="sqlScriptOptions">The sql script options</param>        
        public GzEmbeddedScriptProvider(Assembly assembly, Func<string, bool> filter, Encoding encoding, SqlScriptOptions sqlScriptOptions) : base(new[] { assembly ?? throw new ArgumentNullException(nameof(assembly)) }, filter, encoding, sqlScriptOptions)
        {
        }
    }

    /// <summary>
    /// An <see cref="IScriptProvider"/> implementation which retrieves upgrade scripts embedded in assemblies.
    /// </summary>
    public class GzEmbeddedScriptsProvider : IScriptProvider
    {
        readonly Assembly[] assemblies;
        readonly Encoding encoding;
        readonly Func<string, bool> filter;
        readonly SqlScriptOptions sqlScriptOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="GzEmbeddedScriptsProvider"/> class.
        /// </summary>
        /// <param name="assemblies">The assemblies to search.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="encoding">The encoding.</param>
        public GzEmbeddedScriptsProvider(Assembly[] assemblies, Func<string, bool> filter, Encoding encoding) : this(assemblies, filter, encoding, new SqlScriptOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GzEmbeddedScriptsProvider"/> class.
        /// </summary>
        /// <param name="assemblies">The assemblies to search.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="sqlScriptOptions">The sql script options.</param>
        public GzEmbeddedScriptsProvider(Assembly[] assemblies, Func<string, bool> filter, Encoding encoding, SqlScriptOptions sqlScriptOptions)
        {
            this.assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
            this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            this.sqlScriptOptions = sqlScriptOptions;
        }

        /// <summary>
        /// Gets all scripts that should be executed.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SqlScript> GetScripts(IConnectionManager connectionManager)
        {
            return assemblies
                .Select(assembly => new { Assembly = assembly, ResourceNames = assembly.GetManifestResourceNames().Where(filter).ToArray() })
                .SelectMany(x => x.ResourceNames.Select(resourceName =>
                        SqlScript.FromStream(resourceName,
                        new GZipStream(x.Assembly.GetManifestResourceStream(resourceName), CompressionMode.Decompress),
                        encoding,
                        sqlScriptOptions)))
                .OrderBy(sqlScript => sqlScript.Name)
                .ToList();
        }
    }

}
