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

using System.Reflection;

// By their full names, because inside cloud.charging.open.EMSP both words are
// taken: "CLI" is the namespace this program lives in, and "EMSP" is the
// namespace rather than the class.
using StyxCLI  = org.GraphDefined.Vanaheimr.CLI.CLI;
using Provider = cloud.charging.open.EMSP.EMSP;

#endregion

namespace cloud.charging.open.EMSP.CommandLine
{

    /// <summary>
    /// The command line of a running EMSP.
    /// </summary>
    /// <remarks>
    /// Everything a command needs is reachable from here, which is why every
    /// command takes one of these: the EMSP itself, and through it its
    /// configuration, its log and everything the JSON API can do. A command is
    /// a second way of asking for the same thing as the web interface - never
    /// an implementation of its own.
    ///
    /// Commands are not listed anywhere. The constructor asks Styx to walk this
    /// assembly for anything that implements ICLICommand and can be built from
    /// a ProviderCLI, so a new command is a new file and nothing else. The
    /// vehicle's VehicleCLI and the charging station's StationCLI work the same
    /// way.
    /// </remarks>
    public class ProviderCLI : StyxCLI
    {

        #region Data

        /// <summary>
        /// What stands in front of the command being typed.
        /// </summary>
        public const String Prompt = "EMSP> ";

        #endregion

        #region Properties

        /// <summary>
        /// The EMSP these commands are about.
        /// </summary>
        public Provider  EMSP    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the command line of the given EMSP.
        /// </summary>
        /// <param name="EMSP">The running EMSP.</param>
        /// <param name="AssembliesWithCLICommands">Further assemblies to search for commands. This one is searched either way.</param>
        public ProviderCLI(Provider           EMSP,
                           params Assembly[]  AssembliesWithCLICommands)

            : base(AssembliesWithCLICommands)

        {

            this.EMSP = EMSP;

            RegisterCLIType(typeof(ProviderCLI));

        }

        #endregion


        #region (protected override) GetPrompt()

        /// <summary>
        /// What this program is, because an EMSP usually runs on a bench next to
        /// a CSMS, a charging station and a vehicle, and their consoles should
        /// not have to be told apart by what scrolls past on them.
        /// </summary>
        /// <remarks>
        /// Not its OCPI party: that is written in the banner and in the log, and
        /// in front of every command it would say less than this does - there is
        /// one EMSP on a bench, and it is the one with this prompt.
        /// </remarks>
        protected override String GetPrompt()

            => Prompt;

        #endregion

    }

}
