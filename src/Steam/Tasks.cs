namespace TEKLauncher.Steam;

/// <summary>Defines flags that represent tasks that can be executed by <see cref="Client"/>.</summary>
[Flags]
enum Tasks
{
    GetUpdateData = 1,
    Validate = 1 << 1,
    ReserveDiskSpace = 1 << 2,
    Download = 1 << 3,
    Install = 1 << 4,
    UnpackMod = 1 << 5,
    FinishModInstall = 1 << 6
}