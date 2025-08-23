using System.Runtime.CompilerServices;
using TEKLauncher.Controls;

namespace TEKLauncher.ARK;

/// <summary>Represents a DLC of the game.</summary>
class DLC
{
    /// <summary>Current status of the DLC.</summary>
    Status _status;
    /// <summary>Path to the Content folder of the DLC.</summary>
    readonly string _path;
    /// <summary>Path to the SeekFreeContent folder of the DLC.</summary>
    readonly string _sfcPath;
    /// <summary>Path to the .umap file of the DLC.</summary>
    readonly string _umapPath;
    public readonly uint AppId;
    /// <summary>ID of Steam depot that stores the DLC content.</summary>
    public readonly uint DepotId;
    /// <summary>Code of the map provided by the DLC.</summary>
    public readonly MapCode Code;
    /// <summary>List of all DLC supported by the launcher.</summary>
    public static readonly DLC[] List =
    {
        new("The Center", 473850, 346114, true, false),
        new("Scorched Earth", 512540, 375351, false, true),
        new("Ragnarok", 642250, 375354, true, false),
        new("Aberration", 708770, 375357, false, true),
        new("Extinction", 887380, 473851, false, false),
        new("Valguero", 1100810, 473854, true, true),
        new("Genesis Part 1 & 2", 1113410, 473857, false, false),
        new("Crystal Isles", 1270830, 1318685, true, false),
        new("Lost Island", 1691800, 1691801, true, false),
        new("Fjordur", 1887560, 1887561, true, false),
        new("Aquatica", 3537070, 3537070, false, false)
	};
    /// <summary>Gets a value that indicates whether the DLC is installed.</summary>
    public bool IsInstalled
    {
        get
        {
            bool result = File.Exists(_umapPath);
            if (Code == MapCode.Genesis) //Steam depot of Genesis DLC in fact includes 2 maps, we'll assume that it's installed if at least one of those maps is present
                result |= File.Exists($@"{Game.Path}\ShooterGame\Content\Maps\Genesis2\Gen2.umap");
            return result;
        }
    }
    /// <summary>Gets the display name of the DLC.</summary>
    public string Name { get; }
    /// <summary>Gets or sets current status of the DLC.</summary>
    public Status CurrentStatus
    {
        get => _status;
        set
        {
            _status = value;
            Item?.Dispatcher.Invoke(Item.SetStatus);
        }
    }
    /// <summary>Gets or sets control that represents the DLC in GUI.</summary>
    /// <remarks>This property only has a value if current tab of the main window is DLC tab, otherwise it's <see langword="null"/>.</remarks>
    public DLCItem? Item { get; set; }
    /// <summary>Initializes a new DLC object based on its primary parameters.</summary>
    /// <param name="name">Display name of the DLC.</param>
    /// <param name="depotId">ID of Steam depot that stores the DLC content.</param>
    /// <param name="isMod"><see langword="true"/> if the DLC folders are located in Mods directory rather than Maps; otherwise, <see langword="false"/>.</param>
    /// <param name="has_P"><see langword="true"/> if the name of DLC's .umap file is suffixed with "_P"; otherwise, <see langword="false"/>.</param>
    DLC(string name, uint appId, uint depotId, bool isMod, bool has_P)
    {
        string contentDirectory = isMod ? "Mods" : "Maps";
        string folderName = depotId switch
        {
            473857 => "Genesis",
            1887561 => "FjordurOfficial",
            3537070 => "Abyss",
            _ => name.Replace(" ", string.Empty)
        };
        _path = $@"{Game.Path}\ShooterGame\Content\{contentDirectory}\{folderName}";
        _sfcPath = $@"{Game.Path}\ShooterGame\SeekFreeContent\{contentDirectory}\{folderName}";
        var umapPathBuilder = new StringBuilder(_path);
        umapPathBuilder.Append('\\');
        umapPathBuilder.Append(depotId switch
		{
			1887561 => "Fjordur",
			3537070 => "Aquatica",
			_ => Enum.Parse<MapCode>(folderName)
		});
        if (has_P)
            umapPathBuilder.Append("_P");
        umapPathBuilder.Append(".umap");
        _umapPath = umapPathBuilder.ToString();
        AppId = appId;
        DepotId = depotId;
        Name = name;
        Code = depotId switch
        {
			1887561 => MapCode.Fjordur,
			3537070 => MapCode.Aquatica,
			_ => Enum.Parse<MapCode>(folderName)
		};
        _status = IsInstalled ? Status.Installed : Status.NotInstalled;
    }
    /// <summary>Uninstalls the DLC.</summary>
    public unsafe void Delete()
    {
        CurrentStatus = Status.Deleting;
        var itemId = new TEKSteamClient.ItemId { AppId = 346110, DepotId = DepotId, WorkshopItemId = 0 };
        var desc = TEKSteamClient.AppMng!.GetItemDesc(&itemId);
        var prevStatus = _status;
        CurrentStatus = Status.Deleting;
        if (desc == null)
		{
			if (Directory.Exists(_path))
				Directory.Delete(_path, true);
			if (Directory.Exists(_sfcPath))
				Directory.Delete(_sfcPath, true);
			if (Code == MapCode.Genesis)
			{
				string gen2Folder = $@"{Game.Path}\ShooterGame\Content\Maps\Genesis2";
				if (Directory.Exists(gen2Folder))
					Directory.Delete(gen2Folder, true);
				gen2Folder = $@"{Game.Path}\ShooterGame\SeekFreeContent\Maps\Genesis2";
				if (Directory.Exists(gen2Folder))
					Directory.Delete(gen2Folder, true);
			}
		}
        else
        {
			if (desc->Status.HasFlag(TEKSteamClient.AmItemStatus.Job))
            {
				if (!TEKSteamClient.AppMng.CancelJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(desc)).Success)
                {
                    CurrentStatus = prevStatus;
                    return;
				}
			}
            if (desc->CurrentManifestId != 0)
            {
				if (!TEKSteamClient.AppMng.RunJob(in itemId, ulong.MaxValue, false, null, out desc).Success)
				{
					CurrentStatus = prevStatus;
					return;
				}
			}
		}
        CurrentStatus = Status.NotInstalled;
    }
    /// <summary>Retrieves a DLC by its map code.</summary>
    /// <param name="code">Code of the map whose DLC needs to be retrieved.</param>
    /// <returns>A DLC that provides the specified map.</returns>
    public static DLC Get(MapCode code)
    {
        if (code == MapCode.Genesis2)
            return List[6];
        int index = (int)code - 1;
        if (code > MapCode.Genesis2)
            index--;
        return List[index];
    }
    /// <summary>Defines DLC status codes.</summary>
    public enum Status
    {
        NotInstalled,
        Installed,
        CheckingForUpdates,
        UpdateAvailable,
        Updating,
        Deleting
    }
}