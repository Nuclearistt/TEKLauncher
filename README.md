# TEK Launcher
[![Discord](https://img.shields.io/discord/937821572285206659?style=flat-square&label=Discord&logo=discord&logoColor=white&color=7289DA)](https://discord.gg/47SFqqMBFN)
![Downloads](https://img.shields.io/github/downloads/Nuclearistt/TEKLauncher/total?style=flat-square)

This repository contains the code for TEK Launcher

## What is it?

TEK Launcher is a launcher for ARK: Survival Evolved that can manage game files, DLCs, mods, servers and provides few extra options

## Requirements

+ [.NET 6 Desktop runtime](https://dotnet.microsoft.com/download/dotnet/6.0/runtime)
+ [Steam app](https://store.steampowered.com/about/)

## Key features

+ Built-in Steam CM client implementation to get certain data from Steam servers directly via anonymous account
+ Steam CDN client and downloader/validator built upon it which allows downloading, updating, validating and possibly even downgrading files for any Steam depot as long as its depot key is provided
+ Wrapper upon steamclient64.dll which allows using Steam matchmaking and servers interfaces without initializing the entire Steam API as a game
+ Can make use of [TEK Injector](https://github.com/Nuclearistt/TEKInjector) to make the game ignore Steam's ownership checks and use the mods downloaded by the launcher

## Localizations

Currently supported:
+ English
+ Russian
+ Spanish
+ Portuguese
+ French
+ Greek

If you want to translate the launcher to your language, contact me (Nuclearist) in Discord and I'll provide you with the details. I expect you to be fluent in both English and the language you are going to translate to (or preferably be a native speaker)

## License

TEK Launcher is licensed under the [MIT](LICENSE.TXT) license.
