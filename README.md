<img src="https://camo.githubusercontent.com/d9890324ce9339526a8a6d96ba25e2d63e4e69388d39900472d329f72d18c67e/68747470733a2f2f63646e2e6a756c69616e76732e6465762f6173736574732f6c6f676f2f7a656c64615f6465765f636f6c6f722e706e67" alt="ZeldaDev logo colored." height="150"/>

---

# NetDiscordRpc
A Discord RPC (Rich Presence Client) library written in C#. This is a edited version of [@Lachee](https://github.com/Lachee/discord-rpc-csharp) his Discord Rich Presence package, so some credits of this package goes to him. When trying to use his package a lot of things went wrong, so I build my own, to check if it would work. Below are some of the thing I've edited.

- Removed all "obsoleted" classes, enum values, etc...
- Cleaned up some code.
- Put everything in a separate class instead of a lot of classes in one file.
- Project structure arranged differently
- Added some new methods.

# Issues and/or features.
**Bug**

When having/facing a bug please create a [issue](/issues) with the bug template and describe the issue as good as possible.

**Features**

When you want to have a extra feature added please create a [issue](/issues) with the feature template and describe the feature as good as possible and maybe provide some code as how it would work in your perspective.

# Installation
**Install package**

- Package Manager
```
Install-Package NetDiscordRpc -Version 1.0.2
```
- .NET CLI
```
dotnet add package NetDiscordRpc --version 1.0.2
```
- PackageReference
```
<PackageReference Include="NetDiscordRpc" Version="1.0.2" />
```

Or install it through NuGet:
![NuGet install example](https://camo.githubusercontent.com/cb5ff53765de073c6edf9e26610f584f13fac70736bfe34fb4b7830ddabc7b1e/68747470733a2f2f6d656469612e6a756c69616e76732e6465762f6d656469612f6870794e6a7354615255433872416769584b6c4b2e706e67)

**Required dependencies.**
- Newtonsoft.Json (required)
- Minimal **.NET 5** required.
- C# 9.0

**Creating a application.**
- Go to [Discord Developers](https://discord.com/developers/applications) and create a application.
- Go to the **Rich Presence -> Art assets** tab and add assets etc.
- Go to the tab **OAuth2** and copy your `CLIENT ID`

_On the **Rich Presence -> Visualizer**_ you can see how the Rich Presence will be shown.

**How to use it.**
```cs
class Program
{
    public static DiscordRPC DiscordRpc;
        
    static void Main()
    {
        DiscordRpc = new DiscordRPC("CLIENT_ID_HERE");
        
        // If you want to have everything logged.
        DiscordRpc.Logger = new ConsoleLogger();
        
        // If you want to have nothing logged.
        DiscordRpc.Logger = new NullLogger();
        
        // It is required to initialize.                
        DiscordRpc.Initialize();
                    
        DiscordRpc.SetPresence(new RichPresence()
        {
            Details = "NetDiscordRpc",
            State = "My own Discord RPC C# implementation",
                            
            Assets = new Assets()
            {
                LargeImageKey = "LARGE_IMAGE_KEY_HERE",
                LargeImageText = "LARGE_IMAGE_TEXT_HERE",
                SmallImageKey = "SMALL_IMAGE_KEY_HERE",
                SmallImageText = "SMALL_IMAGE_TEXT_HERE"
            },
            
            Party = new Party()
            {
                ID = Secrets.CreateFriendlySecret(new Random()),
                Size = 1,
                Max = 4,
                Privacy = PartyPrivacySettings.Private
            },
                    
            Timestamps = Timestamps.Now,
                    
            Buttons = new Button[]
            {
                new() { Label = "Website", Url = "https://zeldadev.eu/" }
            }
        });
        
        DiscordRpc.Invoke();
        
        // When using a console application this will prevent it from stopping it.
        Console.ReadKey(true);
    }
}
```

**Good things to know.**

- Secrets cannot be sent with buttons.
- `DiscordRpc.SetSubscription();` does not work when `DiscordRpc.RegisterUriScheme();` has not been set.

**Update presence.**

You can easily update and/or remove presence values without having to re-compile the application by using one of these below methods:

```cs
/* Details section */

// Updating the details with a new value
DiscordRpc.UpdateDetails("New details text here.");

// Removing the details from the presence.
DiscordRpc.UpdateDetails();

/* State section */

// Updating the state with a new value.
DiscordRpc.UpdateState("New state text here.");

// Removing the state from the presence.
DiscordRpc.UpdateState();

/* Large asset section */

// Updating the large assets with new image key and new tooltip.
DiscordRpc.UpdateLargeAsset("Image key", "Image tooltip");

// Update only the large asset key
DiscordRpc.UpdateLargeAsset("Image key");

// Update only the large asset tooltip
DiscordRpc.UpdateLargeAsset(null, "Image tooltip");

// Remove the large assets from the presence.
DiscordRpc.RemoveLargeAsset();

/* Small asset section */

// Updating the small assets with new image key and new tooltip.
DiscordRpc.UpdateSmallAsset("Image key", "Image tooltip");

// Update only the small asset key
DiscordRpc.UpdateSmallAsset("Image key");

// Update only the small asset tooltip
DiscordRpc.UpdateSmallAsset(null, "Image tooltip");

// Remove the small assets from the presence.
DiscordRpc.UpdateSmallAsset();

/* Party section */

// Update the precense with a new party object.
DiscordRpc.UpdateParty(new Party()
{
    ID = "PARTY_ID",
    Size = 1,
    Max = 10,
    Privacy = PartyPrivacySettings.Private
    // Or use PartyPrivacySettings.Public
});

// Update the size of the party.
DiscordRpc.UpdatePartySize(5);

// Update the size of the party and the max party members.
DiscordRpc.UpdatePartySize(5, 10);

// Remove the party from the presence.
DiscordRpc.UpdateParty();

/* Secrets section */

// Update the presence with a new secrets object.
DiscordRpc.UpdateSecrets(new Secrets()
{
    JoinSecret = "JOIN_SECRET",
    SpectateSecret = "SPECTATE_SECRET"
});

// Remove the secret from the presence.
DiscordRpc.UpdateSecrets();

/* Timestamp section */

// Update the presence with a new timestamps object.
DiscordRpc.UpdateTimestamps(new Timestamps()
{
    Start = DateTime.UtcNow,
    End = DateTime.UtcNow + TimeSpan.FromHours(1)
});

// Updates the start time.
DiscordRpc.UpdateStartTime();

// Updates the end time.
DiscordRpc.UpdateEndTime();

// Remove the timestamps from the presence.
DiscordRpc.UpdateClearTime();

/* Button section */

// Update the presence with a new button object.
DiscordRpc.UpdateButtons(new Button[]
{
    new() { Label = "ButtonLabel 1", Url = "https://domain.com" }, // Button 1
    new() { Label = "ButtonLabel 1", Url = "https://domain.com" } // Button 2
});

// Update a single button
DiscordRpc.UpdateButtons(new Button[]
{
    new() { Label = "New button", Url = "https://domain.com" }
}, 1);

// Remove the buttons from the presence.
DiscordRpc.UpdateButtons();

/* Presence section */

// Removes the Rich Presence.
DiscordRpc.ClearPresence();
```

# Contribute
Feel free to contribute to the repository by forking the project and creating a pull request. I will check all pull request as soon as possible.

# License
This project is [MIT](LICENSE) licensed.

# Sponsor me
- [GitHub sponsor](https://github.com/sponsors/julianvschijndel)
- Bitcoin: **3HxQEwRRKigcy2mZXZCHh6ouzVK7HkbMbi**
