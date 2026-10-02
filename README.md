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
several of them when they share a server. What an account may do there is a
matter of its roles - a driver, the operator (`emsp`), a viewer, the
administrators - and the configuration file may add roles of its own; see
[libs/EMSP](libs/EMSP).

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
`--quiet`, `--no-trace`, `--log-file <dir>`, `--no-log-file`, and the four
of the certificate store below. They are every node's switches, read by
WWCP_Node, as is what the console says once the EMSP is up.

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
`--no-log-file` leaves it out, and nothing in it is ever deleted. Which
directory it is, the start says under `log files`.

Below it, `metrological/` is the EMSP's log book: what bears on the time it
stamps things with and on what it trusts. Every start, every synchronisation
with what each time server answered, what was news about a time server's
certificate, every change of its time servers and of its certificates, one
line after the other, each pointing back at the one before and signed with a
key kept beside them. A file per day, never thinned out. Without log files
there is no log book either.


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
the EMSP runs until it is stopped: by Ctrl+C, or by the SIGTERM a service
manager stops it with, which shuts it down just as Ctrl+C does.


### Typing at it over SSH

The same prompt is served over SSH, on port 22355 — twenty thousand above the
web interface's — and on the addresses the web interface listens on: the
loopback, or every address with `--any`. Nothing else is: no shell of the
machine, no files, no tunnels. `--ssh-port` moves it, `--no-ssh` switches it
off.

Whoever signs in is an account of the EMSP, under its name, with a key of its
own. The first start makes `root`; give it your public key once:

```
dotnet run --project EMSPCLI -- --authorize-ssh-key root=C:\Users\you\.ssh\id_ed25519.pub
```

An OpenSSH `.pub` goes in as it is, and so does what PuTTYgen saves with *Save
public key*. The key is kept in `accounts/ssh/root`, a file in the format of
`authorized_keys`, and putting a line into it by hand does the same; taking one
out locks that key out at once. Then:

```
ssh -p 22355 root@127.0.0.1
```

or, in PuTTY, host `127.0.0.1`, port `22355`, *Connection → Data → Auto-login
username* `root`, and the private key under *Connection → SSH → Auth →
Credentials*. The first time, PuTTY asks whether to trust the EMSP's host key:
the banner prints its fingerprint under `SSH`, to compare it with.

Everything works as at the console — Tab, the history, the log above the line
being typed — with three differences. `quit`, `exit` and Ctrl+D leave the
session, and the EMSP keeps running. The account may do what its roles let it
do on the web interface, and the log names it: "'root' at the command line over
SSH asked this EMSP to synchronise its time.", tagged `cli` and `ssh`. And the
session's log starts at the console's level and is its own: `log debug` shows
everything here, `log off` nothing, for this session alone. `who` says who else
is signed in.


### Certificates

Everything this EMSP believes, everything it presents and every server it
recognises lives in one store, `certificates/` beside the configuration file -
so beside the solution unless `--config` says otherwise - and is managed on the
**Certificates** page or from the command line. The store is the directory:
one file per certificate below it, and an `index.json` recording what a file
cannot say about itself: what somebody calls it, whether it is switched on
and - for a TLS root or a server certificate - what it is kept for. So a store
copied to another machine arrives complete, and a lost index costs labels,
switches and usages rather than certificates.

A **tlsRoot** says which time server and which name server over TLS or HTTPS
may be believed, beside the roots of the machine the EMSP runs on - and is told
what it is for, `nts`, `dns` or both, because a root kept for the name servers
alone vouches for no time. A **tlsServer** is a server's own certificate, kept
so that the server can be held to it by its fingerprint. Holding a server to a
certificate or a root is said on the **NTS client** and **DNS client** pages:
a server's dialog takes the fingerprints, offers the one it showed last and
the ones the store keeps for it, and says what a mismatch comes to and whether
it is held to what it is first believed with. What every server was last
believed with is kept in `known-servers.json` beside the configuration file -
fingerprints and nothing else - so that another certificate is noticed where a
server is held to none.

The store keeps the three roots of Plug & Charge as well - `v2gRoot`, `moRoot`
and `oemRoot`, kept apart because one bag of roots would let an OEM root vouch
for a contract - and a `clientRoot` and a `tlsIdentity`, which nothing in the
EMSP uses yet. What only a vehicle holds is refused. The MO root this EMSP
signs its contracts below is not in the store: it is kept with its private key
below `pki/`, see above.

```
dotnet run --project EMSPCLI -- --import-certificate tlsRoot=our-clocks-root.pem --list-certificates
```

A root is believed as soon as it is in - a TLS root or a server certificate for
every use, until the Certificates page says what it is for.
`--list-certificates` prints every handle, and for a TLS root or a server
certificate what it is kept for; `--certificates <dir>` points the EMSP at
another store, a relative directory measured from where the EMSP is started.
PEM, DER and PKCS#12 all go in, and a protected PKCS#12 is opened with
`--certificate-password <pw>` or, better, `EMSP_CERT_PASSWORD` - once, and
the password is not kept.

The store holds private keys **unencrypted**: a PKCS#12 is opened with its
password once, at import, and written back without one. The file system is
what guards them, and the EMSP says so at every start and at every import.
Reading the store is for every role but a driver; changing it is the
administrators', unless the configuration file says otherwise.


### Where things are

| | |
|---|---|
| `EMSPCLI/` | the command line: switches, what the console says at a start, and the prompt in `CLI/` - the node's, with the commands every node has, `syncNTS` among them |
| `libs/EMSP/EMSP/` | the EMSP itself - its sections of the configuration file, its roles, its routes on the node's JSON API, its OCPI bindings, its web interface |
| `libs/EMSP/EMSP/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack |
| `libs/EMSP/EMSP/Contracts/` | the MO root and its sub-CAs, the signing of contracts, the eMAID and its check digit |
| `libs/EMSP/EMSPTests/` | what an EMSP does when a browser, a driver or a CPO talks to it |
| `libs/WWCP_Node/` | the node below the EMSP: the log, the configuration file, name resolution and the time, the certificate store, the accounts, the web server and the JSON API every node answers - what an EMSP has in common with a vehicle and a charging station |
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
