# TEK Launcher
[![Discord](https://img.shields.io/discord/937821572285206659?style=flat-square&label=Discord&logo=discord&logoColor=white&color=7289DA)](https://discord.gg/JBUgcwvpfc)
[![Downloads](https://img.shields.io/github/downloads/Nuclearistt/TEKLauncher/total?style=flat-square)](https://github.com/Nuclearistt/TEKLauncher/releases)

## Overview

TEK Launcher is a launcher for ARK: Survival Evolved that can manage game files, DLCs, mods, servers and provides few extra options

## Requirements

+ [.NET 7 Desktop Runtime](https://aka.ms/dotnet/STS/windowsdesktop-runtime-win-x64.exe)
+ [Steam app](https://store.steampowered.com/about/)

## Key features

+ Uses [ARK Shellcode](https://github.com/Nuclearistt/ARKShellcode) to disable ownership checks in the game and modify its other behaviour if necessary
+ Built-in Steam CM client implementation to get certain data from Steam servers directly via anonymous account
+ Steam CDN client and downloader/validator built upon it which allows downloading, updating and validating files for any Steam depot as long as its depot key is provided
+ Wrapper upon steamclient64.dll which allows using Steam matchmaking/servers interfaces without initializing the entire Steam API on behalf of a game

## Localizations

Currently supported:
+ English
+ Russian
+ Spanish
+ Portuguese
+ French
+ Greek
+ Simplified Chinese
+ Dutch

No new localizations will be added for a while due to my absence

## Note

The launcher won't receive any updates or support until late November-December 2023 due to conscription of the developer

## License

TEK Launcher is licensed under the [MIT](LICENSE.TXT) license.
