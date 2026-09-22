/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EMSP <https://github.com/OpenChargingCloud/EMSP>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.EMSP.Configuration;
using cloud.charging.open.EMSP.Logging;

// Inside this namespace "EMSP" is the namespace and not the class, so the
// class needs a name of its own here.
using Provider = cloud.charging.open.EMSP.EMSP;

#endregion

namespace cloud.charging.open.EMSP.CLI
{

    /// <summary>
    /// One e-mobility service provider, with its web interface, until Ctrl+C.
    /// </summary>
    public class Program
    {

        #region (private static) TryTakeValue(Arguments, ref Index, out Value)

        private static Boolean TryTakeValue(String[]     Arguments,
                                            ref Int32    Index,
                                            out String?  Value)
        {

            if (Index + 1 < Arguments.Length && !Arguments[Index + 1].StartsWith("--"))
            {
                Value = Arguments[++Index];
                return true;
            }

            Value = null;
            return false;

        }

        #endregion

        #region (private static) RepositoryRoot()

        /// <summary>
        /// The directory holding EMSPCLI.slnx, looked up from the binary and
        /// from the current directory; the current directory when neither
        /// leads to it.
        /// </summary>
        /// <remarks>
        /// The accounts, the configuration and the OCPI files beside it
        /// default to a place below it, so that they do not end up in bin/ -
        /// where the next "dotnet clean" would take this EMSP's password and
        /// its roaming partners with it.
        /// </remarks>
        private static String RepositoryRoot()
        {

            foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {

                var directory = new DirectoryInfo(start);

                while (directory is not null)
                {

                    if (File.Exists(Path.Combine(directory.FullName, "EMSPCLI.slnx")))
                        return directory.FullName;

                    directory = directory.Parent;

                }

            }

            return Environment.CurrentDirectory;

        }

        #endregion

        #region (private static) PrintUsage()

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: EMSPCLI [--port <number>] [--any] [--frontend <dist directory>]");
            Console.WriteLine("               [--config <file>] [--accounts <directory>]");
            Console.WriteLine("               [--verbose | --quiet] [--no-trace]");
            Console.WriteLine();
            Console.WriteLine("Web interface:");
            Console.WriteLine($"  --port <number>   TCP port to listen on (default: {Provider.DefaultHTTPPort})");
            Console.WriteLine("  --any             listen on all addresses instead of 127.0.0.1");
            Console.WriteLine("  --frontend <dir>  serve the web interface from a directory on disk instead of the");
            Console.WriteLine("                    bundle embedded in the assembly - use it together with");
            Console.WriteLine("                    'npm run watch' in libs/EMSP/EMSP/Frontend");
            Console.WriteLine();
            Console.WriteLine("Accounts:");
            Console.WriteLine($"  --accounts <dir>    where the accounts live (default: {Provider.DefaultAccountsPath}/ below the");
            Console.WriteLine("                      repository root): the users, their roles, the organizations and");
            Console.WriteLine("                      the API keys. Without them a password is made up at the first");
            Console.WriteLine($"                      start for the user '{Provider.DefaultAdminUser}' and shown once.");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("  --config <file>   where the name servers, the time servers and the OCPI identity of");
            Console.WriteLine($"                    this EMSP live (default: {EMSPConfigFile.DefaultFileName} below the repository");
            Console.WriteLine("                    root). Without the file the EMSP runs on the system defaults and is");
            Console.WriteLine($"                    {OCPIConfiguration.DefaultCountryCode}-{OCPIConfiguration.DefaultPartyId} in OCPI; the DNS and NTS pages of the web interface write");
            Console.WriteLine("                    the name and time servers into it, and every change there takes");
            Console.WriteLine("                    effect at once. Who this EMSP is in OCPI is read once, at the start.");
            Console.WriteLine("                    The roaming partners and the tokens are not in it: the OCPI library");
            Console.WriteLine($"                    keeps them in files of its own below {Provider.OCPIDirectoryName}/ beside it, where the");
            Console.WriteLine("                    web interface puts them.");
            Console.WriteLine();
            Console.WriteLine("Contracts:");
            Console.WriteLine($"  The MO root, its two sub-CAs and every contract certificate issued to a driver live");
            Console.WriteLine($"  below {Provider.PKIDirectoryName}/ beside the configuration file, made at the first start and kept. A");
            Console.WriteLine("  \"contracts\" section of the configuration file may switch the sign-up of drivers off");
            Console.WriteLine("  (\"selfSignUp\": false) and change how long a contract is good for (\"validityDays\").");
            Console.WriteLine();
            Console.WriteLine("Log:");
            Console.WriteLine("  -v, --verbose     write every entry to the console, down to the debug ones");
            Console.WriteLine("  -q, --quiet       write only warnings and worse");
            Console.WriteLine("      --no-trace    do not pick up what the libraries below write with DebugX");
            Console.WriteLine();
            Console.WriteLine("Whatever the console shows, the web interface shows the whole log under 'Logs'.");
        }

        #endregion


        public static async Task<Int32> Main(String[] Arguments)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            #region Arguments

            IPPort?  port            = null;
            var      anyAddress      = false;
            String?  frontendDir     = null;
            String?  configFilePath  = null;
            String?  accountsPath    = null;
            var      verbose         = false;
            var      quiet           = false;
            var      noTrace         = false;

            for (var i = 0; i < Arguments.Length; i++)
            {
                switch (Arguments[i])
                {

                    case "--port":
                        if (i + 1 < Arguments.Length && UInt16.TryParse(Arguments[i + 1], out var parsedPort))
                        {
                            port = IPPort.Parse(parsedPort);
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine("Missing or invalid port number after --port!");
                            return 2;
                        }
                        break;

                    case "--any":
                        anyAddress = true;
                        break;

                    case "--frontend":
                        if (!TryTakeValue(Arguments, ref i, out frontendDir))
                        {
                            Console.Error.WriteLine("Missing directory after --frontend!");
                            return 2;
                        }
                        break;

                    case "--accounts":
                        if (!TryTakeValue(Arguments, ref i, out accountsPath))
                        {
                            Console.Error.WriteLine("Missing directory after --accounts!");
                            return 2;
                        }
                        break;

                    case "--config":
                        if (!TryTakeValue(Arguments, ref i, out configFilePath))
                        {
                            Console.Error.WriteLine("Missing file after --config!");
                            return 2;
                        }
                        break;

                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;

                    case "-q":
                    case "--quiet":
                        quiet = true;
                        break;

                    case "--no-trace":
                        noTrace = true;
                        break;

                    case "-h":
                    case "--help":
                        PrintUsage();
                        return 0;

                    default:
                        Console.Error.WriteLine($"Unknown argument '{Arguments[i]}'!");
                        PrintUsage();
                        return 2;

                }
            }

            if (verbose && quiet)
            {
                Console.Error.WriteLine("--verbose and --quiet ask for opposite things!");
                return 2;
            }

            #endregion

            #region Where the web interface comes from

            // A directory given on the command line wins, so that
            // "npm run watch" beside a running EMSP shows up in the browser on
            // a reload, without rebuilding the C# side.
            IStaticContentSource? frontend = null;

            if (frontendDir is not null)
            {

                if (!Directory.Exists(frontendDir))
                {
                    Console.Error.WriteLine($"The frontend directory '{frontendDir}' does not exist!");
                    return 2;
                }

                frontend = new FileSystemContentSource(frontendDir);

            }

            #endregion

            #region The EMSP

            Provider emsp;

            try
            {
                emsp = new Provider(

                           HTTPHostname:     anyAddress
                                                 ? IPvXAddress.Any
                                                 : IPv4Address.Localhost,

                           HTTPPort:         port,

                           AccountsPath:     accountsPath ?? Path.Combine(RepositoryRoot(), Provider.DefaultAccountsPath),

                           ConfigFile:       new EMSPConfigFile(
                                                 configFilePath ?? Path.Combine(RepositoryRoot(), EMSPConfigFile.DefaultFileName)
                                             ),

                           Frontend:         frontend,

                           ConsoleLogLevel:  verbose ? LogLevel.Debug
                                                 : quiet ? LogLevel.Warning
                                                 : LogLevel.Info,

                           BridgeDebugLog:   !noTrace

                       );
            }
            catch (Exception e)
            {

                Console.Error.WriteLine($"The EMSP could not be set up: {e.Message}");

                // An EMSP that does not come up at all is the one moment the
                // stack trace is worth more than a tidy console.
                if (verbose)
                    Console.Error.WriteLine(e);

                return 1;

            }

            await using (emsp)
            {

                await emsp.Start();

                #region What somebody who just started this needs to know

                Console.WriteLine();
                Console.WriteLine($"  web interface  {emsp.WebInterfaceURL}");
                Console.WriteLine($"  JSON API       {emsp.APIURL}v1/status");
                Console.WriteLine($"  event stream   {emsp.APIURL}v1/events");
                Console.WriteLine($"  HTTPExt API    {emsp.WebInterfaceURL}{Provider.ExtAPIPath.ToString().Trim('/')}/");
                Console.WriteLine($"  frontend from  {emsp.Frontend.Description}");

                var builtFrom = BuiltFrom.Repositories.ToArray();

                if (builtFrom.Length > 0)
                {

                    // One line each, and the whole hash. This is meant to be read
                    // out of a bug report and pasted into a checkout, and an
                    // abbreviation is a thing somebody then has to guess the rest
                    // of. The column is as wide as the longest name rather than a
                    // number picked today, so a repository joining later still
                    // lines up.
                    var width = builtFrom.Max(repository => repository.Repository!.Length);

                    for (var i = 0; i < builtFrom.Length; i++)
                        Console.WriteLine((i == 0 ? "  built from     " : "                 ") +
                                          builtFrom[i].Repository!.PadRight(width) +
                                          "  " +
                                          builtFrom[i].Commit);

                }

                Console.WriteLine($"  configuration  {emsp.ConfigFile.Path}");
                Console.WriteLine($"  accounts       {emsp.ExtAPI.Users.Count()} user(s) in {emsp.AccountsPath}");
                Console.WriteLine($"  sign in at     {emsp.WebInterfaceURL}{Provider.ExtAPIPath.ToString().Trim('/')}/login");
                Console.WriteLine($"  OCPI party     {emsp.PartyIdText} ({emsp.BusinessDetails.Name})");
                Console.WriteLine($"  OCPI versions  {emsp.OCPIVersionsURL} ({String.Join(", ", emsp.OCPIVersions.Select(version => version.Label))})");
                Console.WriteLine($"  roaming        {emsp.RemotePartyCount} partner(s), {emsp.TokenCount} token(s) in {emsp.OCPIDirectory}");
                Console.WriteLine($"  MO root        {emsp.ContractCA.RootTrustPath}{(emsp.ContractCA.WasCreated ? " (made just now)" : "")}");
                Console.WriteLine($"  contracts      {emsp.ContractCount} issued, {emsp.ContractValidity.TotalDays:F0} days each, in {emsp.Contracts.Directory}");
                Console.WriteLine($"  drivers        {(emsp.SelfSignUpEnabled ? $"sign up at {emsp.SignUpURL}" : "sign-up switched off; accounts are made by an administrator")}");
                Console.WriteLine($"  name servers   {(emsp.DNSEnabled ? String.Join(", ", emsp.DNSClient.DNSServers) : "switched off")}");

                #region The time servers

                var bands = emsp.TimeSources.Bands();
                var asked = bands.SelectMany(band => band).ToArray();

                if (asked.Length <= 1)
                    Console.WriteLine($"  time server    {emsp.NTSClient.Hostname}{(emsp.NTSEnabled ? "" : " (switched off)")}");

                else
                {

                    // One line per band, because a band is the unit that is
                    // asked at once - putting two bands on one line would read
                    // as six equal servers when it is two and then four.
                    for (var i = 0; i < bands.Count; i++)
                        Console.WriteLine((i == 0 ? "  time servers   " : "                 ") +
                                          String.Join(", ", bands[i].Select(source => source.Hostname.ToString())) +
                                          (bands.Count > 1 ? $"   (priority {bands[i][0].Priority})" : ""));

                    Console.WriteLine($"                 at least {emsp.TimeSources.MinServers} of them must answer" +
                                      (emsp.NTSEnabled ? "" : " - and NTS is switched off"));

                }

                #endregion

                if (emsp.GeneratedPassword is not null)
                {
                    Console.WriteLine();
                    Console.WriteLine("  ┌─ First start: there were no accounts, so one was made up for you ─────────");
                    Console.WriteLine($"  │  user      {Provider.DefaultAdminUser}");
                    Console.WriteLine($"  │  password  {emsp.GeneratedPassword}");
                    // Named rather than called "a hash", and read from the
                    // implementation rather than typed here, so the box cannot
                    // end up describing a scheme this EMSP no longer uses.
                    // "i=600000" is also how passwords.db writes it down, which
                    // is where somebody checking this will look.
                    Console.WriteLine($"  │  It is shown here once and kept only as a {SecurePassword.PBKDF2SHA256} hash");
                    Console.WriteLine($"  │  over {SecurePassword.DefaultIterations} iterations. Write it down.");
                    Console.WriteLine("  └───────────────────────────────────────────────────────────────────────────");
                }

                Console.WriteLine();
                Console.WriteLine("Press Ctrl+C to stop.");
                Console.WriteLine();

                #endregion

                #region Wait for Ctrl+C

                var stopped = new TaskCompletionSource();

                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true;
                    stopped.TrySetResult();
                };

                await stopped.Task;

                #endregion

            }

            #endregion

            return 0;

        }

    }

}
