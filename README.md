# Samples.QuoteManager
Simple quote manager example

## [Build Run](#build-run)

* Download and install dotnet core 3.1 LTS
  * Mac: <https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.100-macos-x64-installer>

  * Linux instructions: <https://docs.microsoft.com/dotnet/core/install/linux-package-managers>

  * Windows: <https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.100-windows-x64-installer>

* Clone code and build:
```bash
git clone https://github.com/boydc7/Samples.QuoteManager
cd Samples.QuoteManager
dotnet publish -c Release -o publish Samples.QuoteManager/Samples.QuoteManager.csproj
```
* Run (from Samples.QuoteManager folder from above)
```bash
cd publish

# Show usage:
dotnet qm.dll

# Run a simple, naive manager implementation
dotnet qm.dll simple

# Run a more reasonable manager implementation
dotnet qm.dll recommended
```

The console application (if started in a run mode) will simply take input from the command line (as diplayed in the usage, and also copied below) and continue operating until you CTRL+C out of execution.

## [Usage](#usage)

dotnet qm.dll <simple|recommended>

PARAMS:

- simple|recommended
Required.
The specification of the IQuoteManager implementation to use for this execution.
simple = A simple, naive, functional quote manager with minimal guards, thread safefty concerns, etc.
recommended = A reasonable (in my opinion obviously) approach to solving the problem in the given time with no external dependencies


COMMANDS:

Valid commands include:

** Add quotes to the system by typing quote lines:
    quote <id>|<symbol>|<price>|<available volume>|<expiration date as unixtimestamp>

Leaving the ID out will simply generate a quote with a new ID.

For example:
    quote 15c064c7-c585-4ee8-aba4-87461597affd|AMZN|1.23|4321|1591750923
    quote |AMZN|1.23|4321|1591750923

** Submit a buy trade by typing trade lines:
    trade <symbol>|<volume requested>

For example:
    trade AMZN|12

** Get a quote:
    getq <symbol>

** Remove a quote with:
    rmq <quote id>

** Remove all quotes for a symbol with:
    rms <symbol>
 
** DUMP all quotes (if using recommended manager):
    dump <space>

** Import quotes from a file in the above quote format without the leading 'quote' command:
    file <file location>

** Print this usage information:
    usage <space>

