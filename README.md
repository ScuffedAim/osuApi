# osu_API Project README

## Project Overview

The `osuApi` library provides a .NET wrapper for interacting with the [osu! api v2](https://osu.ppy.sh/docs/).

## Project Features

- Supports Beatmap, Score and User api endpoints

## Prerequisites

1. .NET Framework
   - Ensure you have .NET Framework version 8.0 or higher installed on your system.

## Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/osu_api.git

# Install the NuGet packages
dotnet restore
```

## Usage

### Basic Usage Example:

```csharp
using osu_api;
var api = new ApiClient("client id", "client secret");
await api.AuthenticateAsync();
var score = await api.GetScoreAsync("MYID");
Console.WriteLine(score.PP);
```

---

## Version Compatibility

This version of `osu_API` is compatible with .NET Framework versions greater than or equal to 8.0.

## Contributing

Contributions are welcome under the GNU GPLv3 license terms.

## Known Issues

- **API Response Parsing**  
  Ensure your environment includes an internet connection for API requests.

- **Rate Limiting**  

## Legal Notice

By using this library, you agree to the terms of the GNU GPLv3 license.