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

using cloud.charging.open.EMSP.CommandLine;
using cloud.charging.open.EMSP.Configuration;

using cloud.charging.open.protocols.WWCP.Node.CommandLine;
using cloud.charging.open.protocols.WWCP.Node.Configuration;

// Inside this namespace "EMSP" is the namespace and not the class, so the
// class needs a name of its own here.
using Provider = cloud.charging.open.EMSP.EMSP;

#endregion

namespace cloud.charging.open.EMSP.CLI
{

    /// <summary>
    /// One e-mobility service provider, with its web interface and a prompt,
    /// until 'quit', Ctrl+C or SIGTERM.
    /// </summary>
    /// <remarks>
    /// What every kind of node's program does is the node's: the switches and
    /// the words -h explains them with, why it could not be set up or could not
    /// start, what goes into the certificate store, the banner and the prompt.
    /// What is left here is the EMSP's: what its configuration holds, which of
    /// its certificates it uses, where its contracts are, and what its banner
    /// says of OCPI and of the contracts.
    /// </remarks>
    public class Program
    {

        #region (private static) Usage

        /// <summary>
        /// What -h shows: every node's switches, in an EMSP's words, which of
        /// its certificates it uses, and where its contracts are.
        /// </summary>
        private static readonly NodeUsage Usage = new (

            Program:            "EMSPCLI",
            Kind:               Provider.EMSPKind,
            DefaultPort:        Provider.DefaultHTTPPort,
            FrontendSources:    "libs/EMSP/EMSP/Frontend",

            ConfigurationSays:  "where the name servers, the time servers and the OCPI identity of this EMSP live (default: " +
                               $"{WWCPConfigFile.DefaultFileName} below the repository root). Without the file the EMSP runs on " +
                               $"the system defaults and is {OCPIConfiguration.DefaultCountryCode}-{OCPIConfiguration.DefaultPartyId} " +
                                "in OCPI; the DNS and NTS pages of the web interface write the name and time servers into it, " +
                                "and every change there takes effect at once. Who this EMSP is in OCPI is read once, at the " +
                                "start. The roaming partners and the tokens are not in it: the OCPI library keeps them in files " +
                               $"of its own below {Provider.OCPIDirectoryName}/ beside it, where the web interface puts them.",

            CertificateKinds:   Provider.StoredCertificateKinds,
            CertificatesSays:   "v2gRoot, moRoot, oemRoot, clientRoot and tlsIdentity are kept, and used by nothing here yet: " +
                                "no chain is checked against those roots, and nothing presents the identity.",

            BeforeTheLog:       [
                                    "Contracts:",
                                    .. NodeUsage.Wrap("The MO root, its two sub-CAs and every contract certificate issued to a " +
                                                     $"driver live below {Provider.PKIDirectoryName}/ beside the configuration file, " +
                                                      "made at the first start and kept - with their private keys, and so not in " +
                                                      "the certificate store. A \"contracts\" section of the configuration file " +
                                                      "may switch the sign-up of drivers off (\"selfSignUp\": false) and change how " +
                                                      "long a contract is good for (\"validityDays\").",
                                                      "  ",
                                                      "  "),
                                    ""
                                ]

        );

        #endregion


        public static async Task<Int32> Main(String[] Arguments)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            #region Arguments

            // Every node's switches; an EMSP has none of its own.
            var arguments = NodeArguments.Parse(Arguments);

            if (arguments.Refused(Usage) is Int32 refused)
                return refused;

            if (arguments.RefuseTheRest(Usage) is Int32 unknown)
                return unknown;

            // Not bin/, where the next "dotnet clean" would take this EMSP's
            // password with it - nor, since the OCPI files and the contracts
            // go beside the configuration file, its roaming partners and its
            // MO root.
            var root = NodeProgram.RepositoryRoot("EMSPCLI.slnx");

            #endregion

            #region The EMSP

            Provider emsp;

            try
            {
                emsp = new Provider(
                           HTTPHostname:      arguments.HTTPHostname,
                           HTTPPort:          arguments.Port,
                           AccountsPath:      arguments.AccountsPathBelow(root),
                           ConfigFile:        new WWCPConfigFile(arguments.ConfigFilePathBelow(root)),
                           Frontend:          arguments.Frontend,
                           CertificatesPath:  arguments.CertificatesPath,
                           ConsoleLogLevel:   arguments.ConsoleLogLevel,
                           LogPath:           arguments.LogPathBelow(root),
                           BridgeDebugLog:    !arguments.NoTrace,
                           SSH:               arguments.SSH
                       );
            }
            catch (Exception e)
            {
                return NodeProgram.CouldNotBeSetUp(Provider.EMSPKind, e, arguments.Verbose);
            }

            await using (emsp)
            {

                // What somebody signed in over SSH gets: this program's own command
                // line, with its commands beside the node's.
                emsp.CommandLines = (terminal, caller) => new ProviderCLI(emsp, terminal, caller);

                if (emsp.ImportCertificates(arguments, out _) is Int32 notImported)
                    return notImported;

                if (arguments.ListCertificates)
                    emsp.ListCertificates();

                if (await emsp.Started(arguments.Verbose) is Int32 notStarted)
                    return notStarted;

                #region What somebody who just started this needs to know

                foreach (var line in emsp.Banner(
                                         OfTheKind: [
                                             ("OCPI party",     $"{emsp.PartyIdText} ({emsp.BusinessDetails.Name})"),
                                             ("OCPI versions",  $"{emsp.OCPIVersionsURL} ({String.Join(", ", emsp.OCPIVersions.Select(version => version.Label))})"),
                                             ("roaming",        $"{emsp.RemotePartyCount} partner(s), {emsp.TokenCount} token(s) in {emsp.OCPIDirectory}"),
                                             ("MO root",        $"{emsp.ContractCA.RootTrustPath}{(emsp.ContractCA.WasCreated ? " (made just now)" : "")}"),
                                             ("contracts",      $"{emsp.ContractCount} issued, {emsp.ContractValidity.TotalDays:F0} days each, in {emsp.Contracts.Directory}"),
                                             ("drivers",        emsp.SelfSignUpEnabled
                                                                    ? $"sign up at {emsp.SignUpURL}"
                                                                    : "sign-up switched off; accounts are made by an administrator")
                                         ]))
                    Console.WriteLine(line);

                #endregion

                #region The command line, until 'quit', Ctrl+C or SIGTERM

                // The node's: a prompt where somebody can type, and waiting
                // where nobody can, with the log sharing the screen.
                await new ProviderCLI(emsp).RunUntilStopped();

                #endregion

            }

            #endregion

            return 0;

        }

    }

}
