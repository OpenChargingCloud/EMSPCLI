# EMSP - E-Mobility Service Provider

[![CI](https://github.com/OpenChargingCloud/EMSPCLI/actions/workflows/ci.yml/badge.svg)](https://github.com/OpenChargingCloud/EMSPCLI/actions/workflows/ci.yml)
[![Nightly](https://github.com/OpenChargingCloud/EMSPCLI/actions/workflows/nightly.yml/badge.svg)](https://github.com/OpenChargingCloud/EMSPCLI/actions/workflows/nightly.yml)

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
`--quiet`, `--no-trace`, `--log-file <dir>`, `--no-log-file`.

While working on the web interface, run `npm run watch` in
`libs/EMSP/EMSP/Frontend` and start the EMSP with `--frontend
libs/EMSP/EMSP/Frontend/dist`: a reload in the browser then shows the change,
without rebuilding the C# side.


### The log

Everything that happens is written three times over, because the three answer
different questions. The **console** shows what is going on to whoever is
watching, at the level `--verbose` and `--quiet` choose. The **Logs** page
keeps the last two thousand entries for whoever asks, and loses them when the
process ends. And `logs/` beside the solution keeps one file per day,
`emsp-2026-09-25.log`, every entry down to the debug ones, for the afternoon
somebody asks what happened last night - `--log-file <dir>` puts it elsewhere,
`--no-log-file` leaves it out, and nothing in it is ever deleted.


### Typing at it

Once it is up, the console is a prompt rather than a place that only scrolls:

```
EMSP> syncNTS
succeeded after 809 ms: 4 of 4 server(s) answered (2 required), offset +1015.4 ms, spread 2.0 ms
  ptbtime1.ptb.de  +1014.8 ms, round trip 50.7 ms, key exchange new
  ptbtime2.ptb.de  +1015.9 ms, round trip 50.8 ms, key exchange new
  ptbtime3.ptb.de  +1014.9 ms, round trip 50.6 ms, key exchange new
  ptbtime4.ptb.de  +1016.8 ms, round trip 50.5 ms, key exchange new
```

`help` lists what can be typed, `quit` leaves, **Tab** completes and **↑**
walks back through what was typed before. `syncNTS` is **Sync now** on the
**NTS** page, typed: the same group of time servers is asked, the same entries
go into the log, and the same result is left behind for the page to show. The
one entry that differs says who asked - the page names the account that
pressed the button, the prompt says it was somebody at the command line.
`syncNTS <time server>` is that server's **Test** button: every step with when
it happened, the TLS certificate chain down to its root CA among them. Only a
server of this EMSP is tested, and Tab offers them. Neither steps the clock.

The log keeps writing while you type, from whichever thread did the thing it is
reporting, and your half-typed line survives it: the line is taken off the
screen, the entry is written whole, and the line comes back with the cursor
where it was. A line wider than the console is shown through a window onto it.

Where there is no terminal - from a script, under a service manager, in CI, or
with the output going into a file or through `| tee` - there is no prompt, and
the EMSP runs until it is stopped, exactly as it did before.


### Where things are

| | |
|---|---|
| `EMSPCLI/` | the command line: switches, what the console says at a start, and the prompt with its commands in `CLI/` |
| `libs/EMSP/EMSP/` | the EMSP itself - its sections of the configuration file, its roles, its JSON API, its OCPI bindings, its web interface |
| `libs/EMSP/EMSP/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack |
| `libs/EMSP/EMSP/Contracts/` | the MO root and its sub-CAs, the signing of contracts, the eMAID and its check digit |
| `libs/EMSP/EMSPTests/` | what an EMSP does when a browser, a driver or a CPO talks to it |
| `libs/WWCP_Node/` | the node below the EMSP: the log, the configuration file, name resolution and the time, the certificate store, the accounts and the web server - what an EMSP has in common with a vehicle and a charging station |
| `libs/WWCP_OCPI/` | the protocol: OCPI 2.1.1, 2.2.1 and 2.3.0 |
| `libs/WWCP_ISO15118/` | the ISO 15118 certificate profiles the MO root is built to |
| `.github/workflows/` | what runs on every push, and what runs at night |

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
