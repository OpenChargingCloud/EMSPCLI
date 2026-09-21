# EMSP - E-Mobility Service Provider

This software implements an EV roaming E-Mobility Service Provider: the other
end of every OCPI roaming agreement, the thing the charge point operators are
peered with and push their locations, tariffs, sessions and charge detail
records into, with a web interface in front of it. What it is and what it can
be told lives in [libs/EMSP](libs/EMSP); this repository is the command line
that starts it and the submodules it is built from.


### Getting it

The libraries it is built from are submodules, so they have to come along:

```
git clone --recurse-submodules <this repository>
```

If you already cloned it without them:

```
git submodule update --init --recursive
```

They are fetched from GitHub over https, so nothing but git is needed - no
account, no key.

**On Windows**, turn long paths on first:

```
git config --global core.longpaths true
```

The deepest file in the submodules is well over 140 characters below the clone
root, so under the classic 260-character limit the root has little room to live
in. `D:\src\EMSP` is fine; a checkout somewhere below
`C:\Users\<you>\AppData\Local\Temp\...` is not, and the clone fails halfway
through a submodule with `Filename too long` rather than at the start.
Per clone instead of globally: `git clone -c core.longpaths=true ...`.


### Building and running it

```
dotnet build EMSPCLI.slnx
dotnet run --project EMSPCLI
```

The build needs the .NET 10 SDK and Node.js: the web interface is built by npm
and embedded into the assembly, so the EMSP is one thing to deploy. `dotnet
build -p:SkipFrontendBuild=true` leaves the npm step out and reuses whatever is
in `libs/EMSP/EMSP/Frontend/dist`.

At the first start there are no accounts, so the EMSP makes one up for the user
`root`, keeps its hash with the other accounts below `accounts/` beside the
solution and prints the password once. Then open http://127.0.0.1:2355/ and
sign in. Signing in happens at Hermod's HTTPExt API, mounted under `/ext` - the
same door the other components use, which is what lets one sign-in cover
several of them when they share a server.

A roaming partner is given one URL, http://127.0.0.1:2355/ext/versions, and
finds everything else of OCPI from it. Who this EMSP is - `DE-GDF` unless
`configuration.json` beside the solution says otherwise - and which OCPI
versions it offers are read from that file once, at the start. The partners it
is peered with, the tokens it issued and what the partners pushed are kept by
the OCPI library in files of its own below `ocpi/` beside it, where the web
interface puts them.

A driver signs up at http://127.0.0.1:2355/signup and asks the EMSP for a
contract certificate: the browser makes the key pair and never lets it go,
the EMSP signs a certificate to a fresh eMAID below its own MO root, and the
driver downloads a PKCS#12 for the vehicle and `mo-root.pem` for whoever has
to believe it - the vehicle, and the charge point operator. The MO root and
its two sub-CAs are made at the first start and kept below `pki/` beside the
solution, together with every contract issued; the console names the file to
hand out at every start. How this works, and what a `contracts` section of
the configuration file may say, is in [libs/EMSP](libs/EMSP).

`dotnet run --project EMSPCLI -- --help` lists the rest: `--port`, `--any`,
`--accounts <dir>`, `--frontend <dir>`, `--config <file>`, `--verbose`,
`--quiet`, `--no-trace`.

While working on the web interface, run `npm run watch` in
`libs/EMSP/EMSP/Frontend` and start the EMSP with `--frontend
libs/EMSP/EMSP/Frontend/dist`: a reload in the browser then shows the change,
without rebuilding the C# side.


### Where things are

| | |
|---|---|
| `EMSPCLI/` | the command line: switches, and what the console says at a start |
| `libs/EMSP/EMSP/` | the EMSP itself - its configuration, its log, its JSON API, its OCPI bindings, its web interface |
| `libs/EMSP/EMSP/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack |
| `libs/EMSP/EMSP/Contracts/` | the MO root and its sub-CAs, the signing of contracts, the eMAID and its check digit |
| `libs/EMSP/EMSPTests/` | what an EMSP does when a browser, a driver or a CPO talks to it |
| `libs/WWCP_OCPI/` | the protocol: OCPI 2.1.1, 2.2.1 and 2.3.0 |
| `libs/WWCP_ISO15118/` | the ISO 15118 certificate profiles the MO root is built to |

The command line is this program's vocabulary and nothing else. What an EMSP
*is*, and what it does, lives in `libs/EMSP`.


### Your participation

This software is Open Source under the **Affero GPL 3.0 license**.
We appreciate your participation in this ongoing project, and your help to
improve it and the e-mobility ICT in general. If you find bugs, want to
request a feature or send us a pull request, feel free to use the normal
GitHub features to do so. For this please read the Contributor License
Agreement carefully and send us a signed copy or use a similar free and
open license.
